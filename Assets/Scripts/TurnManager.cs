using Assets.Scripts;
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
    private List<Unit> _units = new List<Unit>();

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
    }

    //Rename cause this is a turn, not a tick
    public void Tick()
    {
        if (!IsEndOfTurn())
            return;

        if (_player == null)
            _player = EntityRegistry.FilterEntities<Player>().First();

        if (_units.Count <= 0)
        {
            _units.Add(_player);
            _units.AddRange(EntityRegistry.FilterEntities<Enemy>());
        }

        _isTurnReady = _player.IsTurnReady;
    }


    public IEnumerator ExecuteTurn()
    {
        foreach (Enemy enemy in EntityRegistry.FilterEntities<Enemy>())
            enemy.PlanTurn(_player.CurrentCell);

        CheckStunColdown();
        _executing = true;

        int maxActions = 0;
        foreach (Unit unit in _units)
            if (unit.PlannedActions.Count > maxActions)
                maxActions = unit.PlannedActions.Count;

        for (int i = 0; i < maxActions; i++) //Ticks
        {
            MapGrid.Tick(Time.deltaTime);
            foreach (Unit unit in _units)
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

        foreach (Unit unit in _units)
            unit.ClearPlan();
        _executing = false;
        EventBus.Raise<TurnChangeEvent>(++_currenturn);
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