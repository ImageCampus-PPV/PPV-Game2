using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager
{
    public bool IsPersistance => false;

    private EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private HabilitiesDurationConfiguration HabilitiesDurationConfiguration => ServiceProvider.Instance.GetService<HabilitiesDurationConfiguration>();

    private Dictionary<uint, Action> _playerAction;

    private Dictionary<uint, uint> _stunUnits;

    private uint _currenturn = 1;

    public TurnManager()
    {
        _playerAction = new Dictionary<uint, Action>();

        //In case we don't end up using controllers.
        //This could be trigger by events. 
        //Depending the ID/Enum the event gives as a parameter we execute correct strategy
        _playerAction.Add(0, StunAttackAttack);
        _playerAction.Add(1, Move);

        _stunUnits = new Dictionary<uint, uint>();
    }

    public void Init()
    {
        EventBus.Raise<TurnChangeEvent>(_currenturn);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }

    public void Tick()
    {
        //This could be 2 different controllers trigger by events
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
        {
            if (!IsEndOfTurn())
                return;

            if (Input.GetMouseButtonDown(1))
                _playerAction[1]();
            else if (Input.GetMouseButtonDown(0))
                _playerAction[0]();
        }

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

            foreach (Player player in EntityRegistry.Players)
                if (IsCellNearPlayer(player.CurrentCell, clickedCell))
                {
                    APWallet.ConsumeAP(1);
                    _stunUnits[clickedCell.stander.ID] = _currenturn + HabilitiesDurationConfiguration.stunDuration;
                    EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
                }
        }

        EnemiesTurn();
    }

    private void Move()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
        {
            if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                foreach (Player player in EntityRegistry.Players)
                    player.HandleMovement(clickedCell);

        }

        EnemiesTurn();
    }

    //Should be a part of Player controller
    private bool IsCellNearPlayer(Cell playerCell, Cell cell)
    {
        Vector2Int playerCellPos = playerCell.Coordinates;
        Vector2Int cellPos = cell.Coordinates;

        return (playerCellPos.x + 1 == cellPos.x && playerCellPos.y == cellPos.y) ||
            (playerCellPos.x - 1 == cellPos.x && playerCellPos.y == cellPos.y) ||
            (playerCellPos.x == cellPos.x && playerCellPos.y + 1 == cellPos.y) ||
            (playerCellPos.x == cellPos.x && playerCellPos.y - 1 == cellPos.y);
    }

    //This should be a controller.
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

        CheckStunColdown();

        void CheckStunColdown()
        {
            List<uint> removeFromStunList = new List<uint>();

            foreach (KeyValuePair<uint, uint> stunEntities in _stunUnits)
                if (stunEntities.Value == _currenturn)
                    removeFromStunList.Add(stunEntities.Key);

            foreach (uint key in removeFromStunList)
                _stunUnits.Remove(key);
        }

        EventBus.Raise<TurnChangeEvent>(++_currenturn);
    }
}