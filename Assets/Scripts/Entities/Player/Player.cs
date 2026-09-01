using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    public int PlannedTicks => GetPlannedTickCost();

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private AbilitySystem AbilitySystem => ServiceProvider.Instance.GetService<AbilitySystem>();
    private FloatingTextInstancer FloatingTextInstancer => ServiceProvider.Instance.GetService<FloatingTextInstancer>();
    public int PlannedAPCost => GetPlannedAPCost();
    private ClickActionType _currentActionTypeSelected;



    private Terminal _plannedHackTerminal;
    private int _plannedHackTicks;
    private int _plannedHackAPCost;
    public Terminal PlannedHackTerminal => _plannedHackTerminal;
    HackSystem HackSystem => ServiceProvider.Instance.GetService<HackSystem>();

    private bool _isTurnReady = false;
    public bool IsTurnReady => _isTurnReady;

    public List<Cell> PlannedPath => GetPlannedPath();

    private uint _maxHp = 100;
    private uint _currentHp = 100;
    public uint CurrentHp => _currentHp;

    protected const int MOVEMENT_COST = 1;
    protected const int BREAK_MIN_COST = 3;
    private int _breakPenalty;

    private Vector3 headPos => transform.position + Vector3.up;

    public int BreakPenalty => _breakPenalty;

    public void SetHp(uint hp)
    {
        _currentHp = hp;
        EventBus.Raise<PlayerChangeLifeEvent>(_currentHp);
    }

    public void ReduceHp(uint hpToReduce)
    {
        Vibrate();
        if ((int)_currentHp - hpToReduce <= 0)
            _currentHp = 0;
        else
            _currentHp -= hpToReduce;

        EventBus.Raise<PlayerChangeLifeEvent>(_currentHp);

        if (_currentHp <= 0)
            EventBus.Raise<LevelFailedEvent>();
    }

    public void AddHp(uint hpToAdd)
    {
        _currentHp += hpToAdd;

        if (_currentHp > _maxHp)
            _currentHp = _maxHp;

        EventBus.Raise<PlayerChangeLifeEvent>(_currentHp);
    }

    /////////////////DEBUG///////////////////////
    private Coroutine _shakeCoroutine;

    private void Vibrate()
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(VibrateCoroutine());
    }

    private IEnumerator VibrateCoroutine()
    {
        Vector3 originalPosition = transform.localPosition;

        FloatingTextInstancer.InstantiateText($"Ouch!", headPos, Color.red);

        const float duration = 0.12f;
        const float strength = 0.1f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            transform.localPosition =
                originalPosition + (Vector3)UnityEngine.Random.insideUnitCircle * strength;

            yield return null;
        }

        transform.localPosition = originalPosition;
        _shakeCoroutine = null;
    }
    /////////////////END-DEBUG///////////////////////

    public override void Init()
    {
        base.Init();

        _breakPenalty = 0;
        _currentActionTypeSelected = ClickActionType.Move;
        APWallet.Init();
        AbilitySystem.Init();

        EventBus.Subscribe<MoveButtonEvent>((in MoveButtonEvent callback) => SetClickActionType(ClickActionType.Move));
        EventBus.Subscribe<HackButtonEvent>((in HackButtonEvent callback) => SetClickActionType(ClickActionType.Hack));
        EventBus.Subscribe<KickButtonEvent>((in KickButtonEvent callback) => SetClickActionType(ClickActionType.Kick));
        EventBus.Subscribe<PunchButtonEvent>((in PunchButtonEvent callback) => SetClickActionType(ClickActionType.Punch));
        EventBus.Subscribe<WaitButtonEvent>((in WaitButtonEvent callback) => AddWaitAction());
        EventBus.Subscribe<UndoButtonEvent>(OnUndoButton);
        EventBus.Subscribe<RestartButtonEvent>((in RestartButtonEvent callback) => RestartPlan());
        EventBus.Subscribe<ConfirmActionsButtonEvent>(OnConfirmAction);
        EventBus.Subscribe<EndTurnButtonEvent>(OnEndTurnButton);
    }

    private void SetClickActionType(ClickActionType actionType)
    {
        _currentActionTypeSelected = actionType;
    }

    private void Update()
    {
        if (_isTurnPlaying)
        {
            if (Input.GetKeyUp(KeyCode.Z))
                EventBus.Raise<BreakEvent>();

            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("LEFT INPUT DETECTED");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    ReactToClick(clickedCell);
        }

        if (Input.GetKeyDown(KeyCode.Q))
            TryUseAbility<PunchAbility>();

        if (Input.GetKeyDown(KeyCode.E))
            TryUseAbility<KickAbility>();

        if (Input.GetKeyDown(KeyCode.W))
            AddWaitAction();

        //if (Input.GetKeyDown(KeyCode.F))
        //    TryPlanHack();

        //R removes all actions
        if (Input.GetKeyUp(KeyCode.R))
        {
            RestartPlan();
        }

        //FinishTurn
        if (Input.GetKeyUp(KeyCode.Space))
            _isTurnReady = true;
    }


    private void AddWaitAction()
    {
        WaitAction action = new WaitAction();

        if (!CanAddAction(action))
            return;

        _plannedActions.Add(action);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);
    }

    protected override bool CanAddAction(TurnAction action)
    {
        int futureAP = GetPlannedAPCost() + action.APCost;
        int futureTicks = _usedTicksThisTurn + GetPlannedTickCost() + action.TotalTicks;

   

        if (futureAP > APWallet.CurrentAP)
        {
            FloatingTextInstancer.InstantiateText("Not enough AP", headPos, Color.red);
            return false;
        }

        if (futureTicks > _maxTicksPerTurn)
        {
            FloatingTextInstancer.InstantiateText("Max ticks per turn reached", headPos, Color.red);
            return false;
        }

        return true;
    }

    private bool CanAddActions(List<TurnAction> actions)
    {
        int futureAP = GetPlannedAPCost();
        int futureTicks = _usedTicksThisTurn + GetPlannedTickCost();

        foreach (TurnAction action in actions)
        {
            futureAP += action.APCost;
            futureTicks += action.TotalTicks;
        }

        if (futureAP > APWallet.CurrentAP)
        {
            FloatingTextInstancer.InstantiateText("Not enough AP", headPos, Color.red);
            return false;
        }

        if (futureTicks > _maxTicksPerTurn)
        {
            FloatingTextInstancer.InstantiateText("Max ticks per turn reached", headPos, Color.red);
            return false;
        }

        return true;
    }

    private void ReactToClick(Cell clickedCell)
    {
        switch (_currentActionTypeSelected)
        {
            case ClickActionType.Move:
                {
                    TryMove(clickedCell);
                    break;
                }

            case ClickActionType.Hack:
                {
                    TryPlanHack(clickedCell.Terminal);
                    break;
                }

            case ClickActionType.Kick:
                {
                    TryUseAbility<KickAbility>(clickedCell);
                    break;
                }

            case ClickActionType.Punch:
                {
                    TryUseAbility<PunchAbility>(clickedCell);
                    break;
                }

            default:
                {
                    Debug.LogWarning("Click action type switche entered default");
                    break;
                }
        }


    }

    private void TryMove(Cell clickedCell)
    {
        if (!IsCellAvailable(clickedCell))
        {
            FloatingTextInstancer.InstantiateText("Cell is not available", clickedCell.GetWorldTopPosition(), Color.red);
            return;
        }

        Cell origin = GetLastPlannedCell();
        List<Cell> path = GetPathCells(origin, clickedCell);

        if (path == null || path.Count <= 1)
            return;

        List<TurnAction> newActions = new();

        for (int i = 1; i < path.Count; i++)
        {
            TurnAction action = new MoveAction(path[i - 1], path[i], MOVEMENT_COST + _breakPenalty);
            newActions.Add(action);
        }

        if (!CanAddActions(newActions))
            return;

        _plannedActions.AddRange(newActions);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);
    }

    // --- Terminales / Hackeo ---------------------------------------------

    public bool TryPlanHack(Terminal terminal)
    {
        if (terminal == null)
            return false;

        if (_plannedHackTerminal != null)
        {
            FloatingTextInstancer.InstantiateText("Hack already planned this turn", terminal.Cell.GetWorldTopPosition(), Color.red);
            Debug.Log("[Player] Ya hay un hackeo planificado este turno. Confirma el turno (Space) o cancela el plan (R) antes de planificar otro.");
            return false;
        }

        bool isFreshStart = terminal.CurrentTicks == 0;
        int hackAPCost = isFreshStart ? terminal.APCost : 0;

        int remainingTickBudget = _maxTicksPerTurn - _usedTicksThisTurn - GetPlannedTickCost();
        int ticksNeeded = Mathf.Min(remainingTickBudget, terminal.RequiredTicks - terminal.CurrentTicks);

        if (ticksNeeded <= 0)
        {
            FloatingTextInstancer.InstantiateText($"Not enough ticks for hacking ({GetPlannedTickCost()}/{_maxTicksPerTurn})", terminal.Cell.GetWorldTopPosition(), Color.red);
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

        FloatingTextInstancer.InstantiateText("Hack planned!", terminal.Cell.GetWorldTopPosition(), Color.green);
        Debug.Log($"[Player] Hackeo planificado: {terminal.Type} en {terminal.Cell.Coordinates} ({hackAPCost} AP, {ticksNeeded} ticks). Confirma con Space para ejecutarlo.");

        return true;
    }

    // -----------------------------------------------------------------------

    private void TryUseAbility<T>() where T : class, IAbility
    {
        IAbility ability = AbilitySystem.GetAbility<T>();
        if (ability == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
            return;

        if (!hit.collider.TryGetComponent<Cell>(out Cell targetCell))
            return;

        AbilityAction action = new AbilityAction(this, ability, targetCell, 1, ability.APCost + _breakPenalty);

        if (!CanAddAction(action))
            return;

        if (!ability.CanExecute(this, targetCell))
            return;

        _plannedActions.Add(action);
        FloatingTextInstancer.InstantiateText($"Planned!", targetCell.GetWorldTopPosition(), Color.blue);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);
    }

    private void TryUseAbility<T>(Cell targetCell) where T : class, IAbility
    {
        IAbility ability = AbilitySystem.GetAbility<T>();
        if (ability == null)
            return;

        AbilityAction action = new AbilityAction(this, ability, targetCell, 1, ability.APCost + _breakPenalty);

        if (!CanAddAction(action))
            return;

        if (!ability.CanExecute(this, targetCell))
            return;

        _plannedActions.Add(action);
        FloatingTextInstancer.InstantiateText($"Planned!", targetCell.GetWorldTopPosition(), Color.blue);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);
    }

    private int GetPlannedAPCost()
    {
        int cost = 0;

        foreach (TurnAction action in _plannedActions)
            cost += action.APCost;

        return cost;
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

    private void RestartPlan()
    {
        FloatingTextInstancer.InstantiateText("Reset!", headPos, Color.blue);
        ClearPlan();
    }

    private void OnUndoButton(in UndoButtonEvent callback)
    {
        if (_plannedActions.Count == 0)
            return;

        _plannedActions.RemoveAt(_plannedActions.Count - 1);
        FloatingTextInstancer.InstantiateText("Undo!", headPos, Color.blue);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - GetPlannedAPCost(), APWallet.MaxAP);
    }

    private void OnConfirmAction(in ConfirmActionsButtonEvent callback)
    {
        if (_plannedActions.Count == 0)
            return;

        EventBus.Raise<PlayerExecuteActionEvent>();
    }

    private void OnEndTurnButton(in EndTurnButtonEvent callback)
    {
        _isTurnReady = true;
        _usedTicksThisTurn = 0;
    }

    protected override void OnMovementStarted()
    {
    }

    protected override void OnMovementFinished()
    {
    }

    public override void ConsumeAP(TurnAction action)
    {
        _usedTicksThisTurn += action.TotalTicks;
        EventBus.Raise<APConsumeRequestAceptedEvent>(action.APCost);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }

    public override void Break(in BreakEvent callback)
    {
        base.Break(callback);
        int apToRemove = CalculateBreakCost();
        _breakPenalty++;

        EventBus.Raise<APConsumeRequestAceptedEvent>(apToRemove);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }

    private int CalculateBreakCost()
    {
        int plannedApCost = 0;

        foreach (TurnAction action in _plannedActions)
            plannedApCost += action.APCost;

        return plannedApCost % 4 > BREAK_MIN_COST ? plannedApCost : BREAK_MIN_COST;
    }

    public override void ClearPlan()
    {
        Debug.Log("Clear plan");
        base.ClearPlan();
        ResetVariables();
    }
}
