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

    private HabilitiesDurationConfiguration HabilitiesDurationConfiguration => ServiceProvider.Instance.GetService<HabilitiesDurationConfiguration>();

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
        EventBus.Raise<PlayerChangeLifeEvent>(EntityRegistry.Players.First().Life);
    }

    public void Tick()
    {
        if (!IsEndOfTurn())
            return;

        if (Input.GetMouseButtonUp(1))
        {
            StunAttackAttack();

            EnemiesTurn();
        }
        else
        {
            Player player = EntityRegistry.Players.First();

            if (player.IsTurnReady)
            {
                player.HandleMovement();

                EnemiesTurn();
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
            EnemiesTurn();
    }

    private void StunAttackAttack()
    {
        if (APWallet.CurrentAP == 0)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
        {
            if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                if (clickedCell.stander == null)
                    return;

            Player player = EntityRegistry.Players.First();

            if (IsCellNearUnit(player.CurrentCell, clickedCell, player.AttackRange))
            {
                EventBus.Raise<APConsumeRequestAceptedEvent>(1);
                clickedCell.stander.gameObject.GetComponent<Renderer>().material.color = Color.blue;
                _stunUnits[clickedCell.stander.ID] = _currenturn + 1 + HabilitiesDurationConfiguration.stunDuration;
                clickedCell.stander.Stun();
                EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
            }
        }

        EnemiesTurn();
    }

    private bool IsCellNearUnit(Cell unitCell, Cell cell, int maxDistance)
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
        foreach (Unit unit in EntityRegistry.Units)
            if (unit.IsMoving)
                return false;

        return true;
    }

    public void EnemiesTurn()
    {
        foreach (Enemy enemy in EntityRegistry.Enemies)
            if (!_stunUnits.ContainsKey(enemy.ID))
                foreach (Player player in EntityRegistry.Players)
                    enemy.TakeTurn(player.CurrentCell);

        foreach (HeavyEnemy heavyEnemy in EntityRegistry.HeavyEnemies)
            if (!heavyEnemy.IsStun)
                if (IsCellNearUnit(heavyEnemy.CurrentCell, EntityRegistry.Players.First().CurrentCell, heavyEnemy.AttackRange))
                    EntityRegistry.Players.First().ReduceLife(heavyEnemy.Damage);

        foreach (LightEnemy lightEnemy in EntityRegistry.LightEnemies)
            if (!lightEnemy.IsStun)
                if (IsCellNearUnit(lightEnemy.CurrentCell, EntityRegistry.Players.First().CurrentCell, lightEnemy.AttackRange))
                {
                    if (lightEnemy.IsChargedAttack)
                        EntityRegistry.Players.First().ReduceLife(lightEnemy.Damage);
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
}