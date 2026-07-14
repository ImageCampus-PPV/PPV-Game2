using Assets.Scripts;
using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
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
    private AbilitySystem AbilitySystem => ServiceProvider.Instance.GetService<AbilitySystem>();

    private HabilitiesDurationConfiguration HabilitiesDurationConfiguration => ServiceProvider.Instance.GetService<HabilitiesDurationConfiguration>();
    private LagSpikeAbility _lagSpikeAbility;
    private CounterAbility _counterAbility;

    private Dictionary<uint, uint> _stunUnits;

    private uint _currenturn = 1;


    public TurnManager()
    {
        _stunUnits = new Dictionary<uint, uint>();
    }

    public void Init()
    {
        EventBus.Raise<TurnChangeEvent>(_currenturn);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
        EventBus.Raise<PlayerChangeLifeEvent>(EntityRegistry.FilterEntities<Player>().First().Life);
        EventBus.Subscribe<DevMovePlayerEvent>(OnDevMovePlayer);

        _lagSpikeAbility = new LagSpikeAbility();
        _counterAbility = new CounterAbility();

        AbilitySystem.RegisterAbility(new LagSpikeAbility());
        AbilitySystem.RegisterAbility(new CounterAbility());
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

        if (Input.GetKeyDown(KeyCode.Q))
            TryUseAbility(_lagSpikeAbility);

        if (Input.GetKeyDown(KeyCode.E))
            TryUseAbility(_counterAbility);

        if (Input.GetKeyDown(KeyCode.F))
            TryPlanHack();

        Player player = EntityRegistry.FilterEntities<Player>().First();

        if (player.IsTurnReady)
        {
            player.HandleMovement();
            EnemiesTurn();
            MapGrid.Tick(Time.deltaTime);
        }
        else
        {
            if (player.IsTurnReady)
            {
                player.HandleMovement();

                EnemiesTurn();
                MapGrid.Tick(Time.deltaTime);
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            EnemiesTurn();
            MapGrid.Tick(Time.deltaTime);
        }
    }

    private void TryUseAbility(IAbility ability)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
            return;

        if (!hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
            return;

        Player player = EntityRegistry.FilterEntities<Player>().First();
        AbilitySystem.UseAbility(ability, player, clickedCell);
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

    public void EnemiesTurn()
    {
        foreach (Enemy enemy in EntityRegistry.FilterEntities<Enemy>())
            if (!_stunUnits.ContainsKey(enemy.ID))
                foreach (Player player in EntityRegistry.FilterEntities<Player>())
                    enemy.TakeTurn(player.CurrentCell);

        foreach (HeavyEnemy heavyEnemy in EntityRegistry.FilterEntities<HeavyEnemy>())
            if (!heavyEnemy.IsStun)
                if (IsCellNearUnit(heavyEnemy.CurrentCell, EntityRegistry.FilterEntities<Player>().First().CurrentCell, heavyEnemy.AttackRange))
                    EntityRegistry.FilterEntities<Player>().First().ReduceLife(heavyEnemy.Damage);

        foreach (LightEnemy lightEnemy in EntityRegistry.FilterEntities<LightEnemy>())
            if (!lightEnemy.IsStun)
                if (IsCellNearUnit(lightEnemy.CurrentCell, EntityRegistry.FilterEntities<Player>().First().CurrentCell, lightEnemy.AttackRange))
                {
                    if (lightEnemy.IsChargedAttack)
                        EntityRegistry.FilterEntities<Player>().First().ReduceLife(lightEnemy.Damage);
                    else
                        lightEnemy.ChargeAttack();
                }
                else if (lightEnemy.IsChargedAttack)
                    lightEnemy.UnchargeAttack();

        CheckStunColdown();

        void CheckStunColdown()
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

        EventBus.Raise<TurnChangeEvent>(++_currenturn);
    }

    public void ApplyStun(Unit unit)
    {
        unit.Stun();
        _stunUnits[unit.ID] = _currenturn + 1 + HabilitiesDurationConfiguration.stunDuration;
        unit.gameObject.GetComponent<Renderer>().material.color = Color.blue;
    }
}