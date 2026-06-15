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
        EventBus.Raise<PlayerChangeLifeEvent>(EntityRegistry.FilterEntities<Player>().First().Life);
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
            Player player = EntityRegistry.FilterEntities<Player>().First();

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

            if (IsCellNearUnit(EntityRegistry.FilterEntities<Player>().First().CurrentCell, clickedCell))
            {
                EventBus.Raise<APConsumeRequestAceptedEvent>(1);
                clickedCell.stander.gameObject.GetComponent<Renderer>().material.color = Color.blue;
                _stunUnits[clickedCell.stander.ID] = _currenturn + HabilitiesDurationConfiguration.stunDuration;
                EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
            }
        }

        EnemiesTurn();
    }

    private bool IsCellNearUnit(Cell unitCell, Cell cell)
    {
        Vector2Int playerCellPos = unitCell.Coordinates;
        Vector2Int cellPos = cell.Coordinates;

        return (playerCellPos.x + 1 == cellPos.x && playerCellPos.y == cellPos.y) ||
            (playerCellPos.x - 1 == cellPos.x && playerCellPos.y == cellPos.y) ||
            (playerCellPos.x == cellPos.x && playerCellPos.y + 1 == cellPos.y) ||
            (playerCellPos.x == cellPos.x && playerCellPos.y - 1 == cellPos.y);
    }

    private bool IsEndOfTurn()
    {
        foreach (Unit unit in EntityRegistry.FilterEntities<Unit>())
        {
            if (unit.IsMoving)
                return false;
        }

        return true;
    }

    public void EnemiesTurn()
    {
        foreach (Enemy enemy in EntityRegistry.FilterEntities<Enemy>())
            if (!_stunUnits.ContainsKey(enemy.ID))
                foreach (Player player in EntityRegistry.FilterEntities<Player>())
                    enemy.TakeTurn(player.CurrentCell);

        foreach (HeavyEnemy heavyEnemy in EntityRegistry.FilterEntities<HeavyEnemy>())
        {
            if (!_stunUnits.ContainsKey(heavyEnemy.ID))
                if (IsCellNearUnit(heavyEnemy.CurrentCell, EntityRegistry.FilterEntities<Player>().First().CurrentCell))
                    EntityRegistry.FilterEntities<Player>().First().ReduceLife(heavyEnemy.Damage);
        }

        CheckStunColdown();

        void CheckStunColdown()
        {
            List<uint> removeFromStunList = new List<uint>();

            foreach (KeyValuePair<uint, uint> stunEntities in _stunUnits)
                if (stunEntities.Value == _currenturn)
                    removeFromStunList.Add(stunEntities.Key);

            foreach (uint key in removeFromStunList)
            {
                EntityRegistry.GetAs<Unit>(key).GetComponent<Renderer>().material.color = Color.red;
                _stunUnits.Remove(key);
            }
        }

        EventBus.Raise<TurnChangeEvent>(++_currenturn);
    }
}