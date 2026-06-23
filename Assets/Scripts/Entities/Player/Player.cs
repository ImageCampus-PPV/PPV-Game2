using Assets.Scripts.Combat;
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
    }

    private void Awake()
    {
        Debug.Log($"Spawn Cell: {_spawnCell.gameObject}");
    }
    private void Update()
    {
        if (IsMoving)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    ReactToInput(clickedCell);
        }

        //Right click removes last waypoint
        if (Input.GetMouseButtonUp(1))
        {
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

            EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
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
                EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
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

        if (_plannedPath.Count <= 0)
            return;

        if (_isMoving)
            return;

        Debug.Log("HANDLE MOVEMENT CALLED");
        //RequestPath(_selectedTargetCell);
        StartCoroutine(FollowPath(_plannedPath));
        EventBus.Raise<APConsumeRequestAceptedEvent>(_plannedAPCost);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
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