using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    [SerializeField] private int _maxTicksPerTurn = 7;
    private int _plannedTicks;
    public int MaxTicksPerTurn => _maxTicksPerTurn;
    public int PlannedTicks => _plannedTicks;
    APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private List<Cell> _waypoints = new List<Cell>();
    private List<Cell> _plannedPath = new List<Cell>();
    private int _plannedAPCost;
    public int PlannedAPCost => _plannedAPCost;

    private Terminal _plannedHackTerminal;
    private int _plannedHackTicks;
    private int _plannedHackAPCost;
    public Terminal PlannedHackTerminal => _plannedHackTerminal;
    HackSystem HackSystem => ServiceProvider.Instance.GetService<HackSystem>();

    private bool _isTurnReady = false;
    public bool IsTurnReady => _isTurnReady;

    public List<Cell> PlannedPath => _plannedPath;
    public List<Cell> Waypoints => _waypoints;

    private uint _life = 100;
    public uint Life => _life;

    public Cell CurrentPlanningOrigin
    {
        get
        {
            if (_waypoints.Count == 0)
                return CurrentCell;

            return _waypoints[_waypoints.Count - 1];
        }
    }

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
    }

    public void AddLife(uint life)
    {
        _life += life;

        EventBus.Raise<PlayerChangeLifeEvent>(_life);

    }

    public override void Init()
    {
        base.Init();
        ServiceProvider.Instance.GetService<EntityRegistry>().Add(this);
    }

    private void Update()
    {
        if (IsMoving)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("LEFT INPUT DETECTED");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    ReactToInput(clickedCell);
        }

        //Right click removes last waypoint
        if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("RIGHHT INPUT DETECTED");

            Cell previousWaypoint;
            Cell waypointToRemove;
            if (_waypoints.Count == 1)
            {
                previousWaypoint = _currentCell;
                waypointToRemove = _waypoints[_waypoints.Count - 1];
                RemoveWaypoint(previousWaypoint, waypointToRemove);
            }
            else if (_waypoints.Count > 1)
            {
                previousWaypoint = _waypoints[_waypoints.Count - 2];
                waypointToRemove = _waypoints[_waypoints.Count - 1];

                RemoveWaypoint(previousWaypoint, waypointToRemove);
            }

            RaiseAPPreview();
        }

        //R removes all waypoints
        if (Input.GetKeyUp(KeyCode.R))
            ResetVariables();

        if (Input.GetKeyUp(KeyCode.Space)) //FinishTurn
            _isTurnReady = true;


        Debug.Log("Planned ticks: " + _plannedTicks);
        
        for (int i = 0; i < _waypoints.Count - 1; i++)
            Debug.Log("Waypoint " + i + ": " + _waypoints[i].Coordinates);
        
        for (int i = 0; i < _plannedPath.Count - 1; i++)
            Debug.Log("Path cell " + i + ": " + _plannedPath[i].Coordinates);

        //List<Cell> test = GetPathCells(_currentCell, _waypoints[0]);
        //
        //foreach (Cell c in test)
        //    Debug.Log(c.Coordinates);

    }


    private void ReactToInput(Cell clickedCell)
    {
        //if (_selectedTargetCell == clickedCell && clickedCell != _currentCell)
        //{
        //    _isTurnReady = true;
        //    return;
        //}

        if (IsCellAvailable(clickedCell))
        {
            Cell originCell = (_waypoints.Count == 0) ? _currentCell : _waypoints[_waypoints.Count - 1];
            List<Cell> cellsToAdd = GetPathCells(originCell, clickedCell);
            int newTicksCount = cellsToAdd.Count - 1;

            int totalTicksUsed = newTicksCount + _plannedTicks;

            int segmentCost = GetPathCost(originCell, clickedCell);
            int totalAPCost = _plannedAPCost + segmentCost;

            if (totalAPCost <= APWallet.CurrentAP && totalTicksUsed <= _maxTicksPerTurn)
            {
                _plannedAPCost += segmentCost;
                //EventBus.Raise<APConsumeRequestAceptedEvent>(_plannedAPCost);
                RaiseAPPreview();
                //_plannedPath = GetPathCells(clickedCell);

                _waypoints.Add(clickedCell);
                RebuildPath();
                _plannedTicks += newTicksCount;
            }

        }

        //if (IsCellAvailable(clickedCell))
        //{
        //    if (GetPathCost(clickedCell) <= APWallet.CurrentAP)
        //    {
        //        _plannedAPCost = GetPathCost(clickedCell);
        //        EventBus.Raise<APConsumeRequestAceptedEvent>(_plannedAPCost);
        //        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
        //        _plannedPath = GetPathCells(clickedCell);
        //        _selectedTargetCell = clickedCell;
        //    }
        //}
        //else if(clickedCell == _currentCell)
        //{
        //    _plannedAPCost = 0;
        //    EventBus.Raise<APConsumeRequestAceptedEvent>(_plannedAPCost);
        //    EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
        //    _plannedPath.Clear();
        //    _selectedTargetCell = clickedCell;
        //}
        //else
        //    _selectedTargetCell = null;



    }

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

        int remainingTickBudget = _maxTicksPerTurn - _plannedTicks;
        int ticksNeeded = Mathf.Min(remainingTickBudget, terminal.RequiredTicks - terminal.CurrentTicks);

        if (ticksNeeded <= 0)
        {
            Debug.Log($"[Player] No queda presupuesto de ticks este turno para hackear ({_plannedTicks}/{_maxTicksPerTurn} ya planificados). Confirma el turno o saca movimiento planificado.");
            return false;
        }

        int totalPlannedAPCost = _plannedAPCost + _plannedHackAPCost + hackAPCost;

        if (!HackSystem.CanStartHack(CurrentPlanningOrigin, terminal, totalPlannedAPCost))
            return false;

        _plannedHackTerminal = terminal;
        _plannedHackTicks = ticksNeeded;
        _plannedHackAPCost = hackAPCost;
        _plannedTicks += ticksNeeded;

        RaiseAPPreview();

        Debug.Log($"[Player] Hackeo planificado: {terminal.Type} en {terminal.Cell.Coordinates} ({hackAPCost} AP, {ticksNeeded} ticks). Confirma con Space para ejecutarlo.");

        return true;
    }

    private void RaiseAPPreview()
    {
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost - _plannedHackAPCost, APWallet.MaxAP);
    }

    private void RebuildPath()
    {
        _plannedPath.Clear();

        Cell current = _currentCell;

        foreach (Cell waypoint in _waypoints)
        {
            List<Cell> segment = GetPathCells(current, waypoint);

            if (segment.Count > 0)
                segment.RemoveAt(0);

            _plannedPath.AddRange(segment);

            current = waypoint;
        }
    }

    public void HandleMovement()
    {
        _isTurnReady = false;

        if (_isMoving)
            return;

        bool hasMovement = _plannedPath.Count > 0;
        bool hasHack = _plannedHackTerminal != null;

        if (!hasMovement && !hasHack)
            return;

        Debug.Log("HANDLE MOVEMENT CALLED");

        if (hasMovement)
        {
            //RequestPath(_selectedTargetCell);
            StartCoroutine(FollowPath(_plannedPath));
            EventBus.Raise<APConsumeRequestAceptedEvent>(_plannedAPCost);
        }

        if (hasHack)
            HackSystem.ResolvePlannedHack(this, _plannedHackTerminal, _plannedHackTicks);

        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);

        if (!hasMovement)
            ResetVariables();
    }

    private void RemoveWaypoint(Cell previousWaypoint, Cell waypointToRemove)
    {
        _plannedAPCost -= GetPathCost(previousWaypoint, waypointToRemove);
        _plannedTicks -= GetPathTicksPreview(previousWaypoint, waypointToRemove);
        _waypoints.RemoveAt(_waypoints.Count - 1);
        RebuildPath();
    }

    private void ResetVariables()
    {
        _isTurnReady = false;
        _plannedPath.Clear();
        _plannedTicks = 0;
        _plannedAPCost = 0;
        _waypoints.Clear();
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

    protected override void OnMovementStarted()
    {
    }

    protected override void OnMovementFinished()
    {
        ResetVariables();
    }
}