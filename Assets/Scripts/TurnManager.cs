using Assets.Scripts;
using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
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
        if (!IsEndOfTurn())
            return;

        if (_player == null)
            _player = EntityRegistry.FilterEntities<Player>().First();


        if (Input.GetKeyDown(KeyCode.F))
            TryPlanHack();

        Player player = EntityRegistry.FilterEntities<Player>().First();

        //if (player.IsTurnReady)
        //{
        //    _units.Add(_player);
        //    _units.AddRange(EntityRegistry.FilterEntities<Enemy>());
        //}

        _isTurnReady = _player.IsTurnReady;
    }


    public IEnumerator ExecuteTurn()
    {
        foreach (Enemy enemy in EntityRegistry.FilterEntities<Enemy>())
            enemy.PlanTurn(_player.CurrentCell);

        CheckStunColdown();
        _executing = true;

        int maxActions = 0;
        
        foreach (Unit unit in EntityRegistry.FilterEntities<Unit>())
            if (unit.PlannedActions.Count > maxActions)
                maxActions = unit.PlannedActions.Count;

        for (int i = 0; i < maxActions; i++) //Ticks
        {
            MapGrid.Tick(Time.deltaTime);

            foreach (Unit unit in EntityRegistry.FilterEntities<Unit>())
            {
                if (unit.IsStun)
                    continue;

                if (i >= unit.PlannedActions.Count)
                    continue;

                IEnumerator routine = unit.PlannedActions[i].Execute(unit);
                unit.ConsumeAP(unit.PlannedActions[i]);

                while (routine.MoveNext())
                    yield return routine.Current;
            }
        }

        foreach (Unit unit in EntityRegistry.FilterEntities<Unit>())
            unit.ClearPlan();
        _executing = false;
        EventBus.Raise<TurnChangeEvent>(++_currenturn);

        if (APWallet.CurrentAP <= 0)
            EventBus.Raise<LevelFailedEvent>();
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

    private bool IsEndOfTurn()
    {
        foreach (Unit unit in EntityRegistry.FilterEntities<Unit>())
            if (unit.IsMoving)
                return false;

        return true;
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
        _stunUnits[unit.ID] = _currenturn + 1 + AbilitiesDurationConfiguration.stunDuration;
        unit.gameObject.GetComponent<Renderer>().material.color = Color.blue;
        Debug.Log("EnemyStunned");
    }
}