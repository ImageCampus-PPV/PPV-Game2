using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    public int PlannedTicks => GetPlannedTickCost();

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private AbilitySystem AbilitySystem => ServiceProvider.Instance.GetService<AbilitySystem>();

    public int PlannedAPCost => GetPlannedAPCost();

    private Terminal _plannedHackTerminal;
    private int _plannedHackTicks;
    private int _plannedHackAPCost;
    public Terminal PlannedHackTerminal => _plannedHackTerminal;
    HackSystem HackSystem => ServiceProvider.Instance.GetService<HackSystem>();

    private bool _isTurnReady = false;
    public bool IsTurnReady => _isTurnReady;

    public List<Cell> PlannedPath => GetPlannedPath();

    private uint _life = 100;
    public uint Life => _life;

    public void SetLife(uint life)
    {
        _life = life;
        EventBus.Raise<PlayerChangeLifeEvent>(_life);
    }

    public void ReduceLife(uint life)
    {
        if ((int)_life - life <= 0)
            _life = 0;
        else
            _life -= life;

        EventBus.Raise<PlayerChangeLifeEvent>(_life);

        if (_life <= 0)
            EventBus.Raise<LevelFailedEvent>();
    }

    public void AddLife(uint life)
    {
        _life += life;
        EventBus.Raise<PlayerChangeLifeEvent>(_life);
    }

    public override void Init()
    {
        base.Init();
        APWallet.Init();
        AbilitySystem.RegisterAbility(new LagSpikeAbility());
        AbilitySystem.RegisterAbility(new CounterAbility());
    }

    private void Update()
    {
        if (_isTurnPlaying)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("LEFT INPUT DETECTED");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    ReactToClick(clickedCell);
        }

        if (Input.GetKeyDown(KeyCode.Q))
            TryUseAbility(new LagSpikeAbility());

        if (Input.GetKeyDown(KeyCode.E))
        { 
            TryUseAbility(new CounterAbility());
            Time.timeScale = 0.1f;
        }

        //R removes all actions
        if (Input.GetKeyUp(KeyCode.R))
            ResetVariables();

        //FinishTurn
        if (Input.GetKeyUp(KeyCode.Space))
            _isTurnReady = true;
    }


    private void ReactToClick(Cell clickedCell)
    {
        if (!IsCellAvailable(clickedCell))
            return;

        Cell origin = GetLastPlannedCell();
        List<Cell> path = GetPathCells(origin, clickedCell);

        if (path == null || path.Count <= 1)
            return;

        int futureAP = GetPlannedAPCost();
        int futureTicks = GetPlannedTickCost();

        List<MoveAction> newActions = new();

        for (int i = 1; i < path.Count; i++)
        {
            int apCost = GetPathCost(path[i - 1], path[i]);

            MoveAction action = new MoveAction(path[i - 1], path[i], apCost);

            futureAP += action.APCost;
            futureTicks += action.TotalTicks;

            newActions.Add(action);
        }

        if (futureAP > APWallet.CurrentAP)
            return;

        if (futureTicks > _maxTicksPerTurn)
            return;

        _plannedActions.AddRange(newActions);

        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - futureAP, APWallet.MaxAP);
    }

    // --- Terminales / Hackeo ---------------------------------------------

    public bool TryPlanHack(Terminal terminal)
    {
        if (terminal == null)
            return false;

        if (_plannedHackTerminal != null)
        {
            Debug.Log("[Player] Ya hay un hackeo planificado este turno. Confirma el turno (Space) o cancela el plan (R) antes de planificar otro.");
            return false;
        }

        bool isFreshStart = terminal.CurrentTicks == 0;
        int hackAPCost = isFreshStart ? terminal.APCost : 0;

        int remainingTickBudget = _maxTicksPerTurn - GetPlannedTickCost();
        int ticksNeeded = Mathf.Min(remainingTickBudget, terminal.RequiredTicks - terminal.CurrentTicks);

        if (ticksNeeded <= 0)
        {
            Debug.Log($"[Player] No queda presupuesto de ticks este turno para hackear ({GetPlannedTickCost()}/{_maxTicksPerTurn} ya planificados). Confirma el turno o saca movimiento planificado.");
            return false;
        }

        int totalPlannedAPCost = GetPlannedAPCost() + hackAPCost;

        Cell originCell = GetLastPlannedCell();

        if (!HackSystem.CanStartHack(originCell, terminal, totalPlannedAPCost))
            return false;

        _plannedHackTerminal = terminal;
        _plannedHackTicks = ticksNeeded;
        _plannedHackAPCost = hackAPCost;

        HackAction hackAction = new HackAction(this, terminal, ticksNeeded, hackAPCost);
        _plannedActions.Add(hackAction);

        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);

        Debug.Log($"[Player] Hackeo planificado: {terminal.Type} en {terminal.Cell.Coordinates} ({hackAPCost} AP, {ticksNeeded} ticks). Confirma con Space para ejecutarlo.");

        return true;
    }

    // -----------------------------------------------------------------------

    private void TryUseAbility(IAbility ability)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
            return;

        if (!hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
            return;

        //AbilitySystem.UseAbility(ability, this, clickedCell);
        AbilityAction abilityAction = new AbilityAction(this, ability, clickedCell, 1, ability.APCost);
        _plannedActions.Add(abilityAction);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);
    }

    private int GetPlannedAPCost()
    {
        int cost = 0;

        foreach (TurnAction action in _plannedActions)
            cost += action.APCost;

        return cost;
    }

    private int GetPlannedTickCost()
    {
        int ticks = 0;

        foreach (TurnAction action in _plannedActions)
            ticks += action.TotalTicks;

        return ticks;
    }

    public Cell GetLastPlannedCell()
    {
        Cell current = CurrentCell;

        foreach (TurnAction action in _plannedActions)
            if (action is MoveAction move)
                current = move.TargetCell;

        return current;
    }

    private void ResetVariables()
    {
        _isTurnReady = false;
        _plannedHackTerminal = null;
        _plannedHackTicks = 0;
        _plannedHackAPCost = 0;
        _plannedActions.Clear();
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }

    public int GetPathCostPreview(Cell origin, Cell target)
    {
        return GetPathCost(origin, target);
    }

    public int GetPathTicksPreview(Cell origin, Cell target)
    {
        List<Cell> path = GetPathCells(origin, target);

        if (path == null)
            return int.MaxValue;

        return path.Count - 1;
    }

    private List<Cell> GetPlannedPath()
    {
        List<Cell> plannedPath = new List<Cell>();
        foreach (TurnAction action in _plannedActions)
            if (action is MoveAction move)
                plannedPath.Add(move.TargetCell);

        return plannedPath;
    }

    protected override void OnMovementStarted()
    {
    }

    protected override void OnMovementFinished()
    {
    }

    public override void ConsumeAP(TurnAction action)
    {
        EventBus.Raise<APConsumeRequestAceptedEvent>(action.APCost);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }

    public override void ClearPlan()
    {
        Debug.Log("Clear plan");
        ResetVariables();
    }
}
