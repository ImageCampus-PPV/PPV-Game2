using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : IService
{
    public bool IsPersistance => false;

    private EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    private AbilitiesDurationConfiguration AbilitiesDurationConfiguration => ServiceProvider.Instance.GetService<AbilitiesDurationConfiguration>();

    private Dictionary<uint, uint> _stunUnits;

    private uint _currenturn = 1;
    private bool _executing;
    public bool IsExecuting => _executing;
    private Player _player;


    public TurnManager()
    {
        _stunUnits = new Dictionary<uint, uint>();
    }

    private bool _isTurnReady;
    public bool IsTurnReady => _isTurnReady;

    public void Init()
    {
        EventBus.Raise<TurnChangeEvent>(_currenturn);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
        EventBus.Raise<PlayerChangeLifeEvent>(EntityRegistry.FilterEntities<Player>().First().Life);
        EventBus.Subscribe<DevMovePlayerEvent>(OnDevMovePlayer);
    }

    private void OnDevMovePlayer(in DevMovePlayerEvent movePlayerEvent)
    {
        if (movePlayerEvent.coordX < 0 || movePlayerEvent.coordX >= MapGrid.Width ||
            movePlayerEvent.coordY < 0 || movePlayerEvent.coordY >= MapGrid.Height)
            return;

        Cell target = MapGrid.GetCell(movePlayerEvent.coordX, movePlayerEvent.coordY);

        if (target == null)
            return;

        EntityRegistry.FilterEntities<Player>().First().MoveInstant(target);
    }

    public void Tick()
    {
        if (_executing)
            return;

        if (_player == null)
            _player = EntityRegistry.FilterEntities<Player>().First();

        if (Input.GetKeyDown(KeyCode.F))
                TryPlanHack();

        _isTurnReady = _player.IsTurnReady;
    }


    public IEnumerator ExecuteTurn()
    {
        _executing = true;
        CheckStunColdown();

        _player.IsTurnPlaying = true;

        foreach (TurnAction action in _player.PlannedActions)
        {
            if (_player.IsStun)
                break;

            _player.ConsumeAP(action);

            IEnumerator routine = action.Execute(_player);

            while (routine.MoveNext())
                yield return routine.Current;

            MapGrid.Tick(Time.deltaTime);

            if (APWallet.CurrentAP <= _player.BreakPenalty)
            {
                EventBus.Raise<LevelFailedEvent>();
                _player.IsTurnPlaying = false;
                _executing = false;
                yield break;
            }
        }

        _player.IsTurnPlaying = false;


        foreach (Enemy enemy in EntityRegistry.FilterEntities<Enemy>())
        {
            if (enemy.IsStun)
                continue;

            enemy.PlanTurn(_player.CurrentCell, 0);
            enemy.IsTurnPlaying = true;

            foreach (TurnAction action in enemy.PlannedActions)
            {
                if (enemy.IsStun)
                    break;

                IEnumerator routine = action.Execute(enemy);

                while (routine.MoveNext())
                    yield return routine.Current;

                MapGrid.Tick(Time.deltaTime);
            }

            enemy.IsTurnPlaying = false;
            enemy.ClearPlan();
            enemy.ResetActionsCounter();
        }


        _player.ClearPlan();
        _player.ResetActionsCounter();

        _executing = false;

        EventBus.Raise<TurnChangeEvent>(++_currenturn);
    }

    private void TryPlanHack()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
        {
            Debug.Log("[TurnManager] F: no hay ninguna celda bajo el cursor.");
            return;
        }

        if (!hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
            return;

        if (clickedCell.Terminal == null)
        {
            Debug.Log($"[TurnManager] F: la celda {clickedCell.Coordinates} bajo el cursor no tiene ninguna Terminal.");
            return;
        }

        Player player = EntityRegistry.FilterEntities<Player>().First();
        player.TryPlanHack(clickedCell.Terminal);
    }

    public bool IsCellNearUnit(Cell unitCell, Cell cell, int maxDistance)
    {
        Vector2Int origin = unitCell.Coordinates;
        Vector2Int target = cell.Coordinates;

        Vector2Int[] directions = new Vector2Int[]
        {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        };

        foreach (Vector2Int dir in directions)
        {
            for (int i = 1; i <= maxDistance; i++)
            {
                Vector2Int current = origin + dir * i;

                if (current.x < 0 || current.y < 0 || current.x >= MapGrid.Width || current.y >= MapGrid.Height)
                    break;

                Cell currentCell = MapGrid.GetCell(current);

                if (currentCell.ProvidesCover)
                    break;

                if (current == target)
                    return true;
            }
        }

        return false;
    }

    private void CheckStunColdown()
    {
        List<uint> removeFromStunList = new List<uint>();

        foreach (KeyValuePair<uint, uint> stunEntity in _stunUnits)
            if (stunEntity.Value == _currenturn)
            {
                EntityRegistry.GetAs<Unit>(stunEntity.Key).GetComponent<Renderer>().material.color = Color.red;
                EntityRegistry.GetAs<Unit>(stunEntity.Key).Unstun();
                removeFromStunList.Add(stunEntity.Key);
            }

        foreach (uint key in removeFromStunList)
            _stunUnits.Remove(key);
    }

    public void ApplyStun(Unit unit)
    {
        unit.Stun();
        unit.ClearPlan();
        _stunUnits[unit.ID] = _currenturn + 1 + AbilitiesDurationConfiguration.stunDuration;
        unit.gameObject.GetComponent<Renderer>().material.color = Color.blue;
        Debug.Log("EnemyStunned");
    }
}