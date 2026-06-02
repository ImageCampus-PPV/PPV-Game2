using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private List<Cell> _plannedPath;
    private int _plannedAPCost;
    private Cell _selectedTargetCell = null;

    private bool _isTurnReady = false;
    public bool IsTurnReady => _isTurnReady;

    //FOR PATH DEBUGING ONLY
    MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    public Cell SelectedTargetCell => _selectedTargetCell;
    public List<Cell> PlannedPath => _plannedPath;

    private uint _life = 100;
    public uint Life => _life;

    public void SetLife(uint life)
    {
        _life = life;
    }

    public void ReduceLife(uint life)
    {
        _life -= life;
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

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    ReactToInput(clickedCell);
        }
    }

    private void ReactToInput(Cell clickedCell)
    {
        if (_selectedTargetCell == clickedCell)
        {
            _isTurnReady = true;
            return;
        }


        if (IsCellAvailable(clickedCell))
        {
            if (GetPathCost(clickedCell) <= APWallet.CurrentAP)
            {
                _plannedAPCost = GetPathCost(clickedCell);
                _plannedPath = GetPathCells(clickedCell);
                _selectedTargetCell = clickedCell;
            }
        }
        else
            _selectedTargetCell = null;

    }


    public void HandleMovement()
    {
        if (_selectedTargetCell == null)
            return;

        if (_isMoving)
            return;

        Debug.Log("HANDLE MOVEMENT CALLED");
        _isTurnReady = false;
        RequestPath(_selectedTargetCell);
        EventBus.Raise<APConsumeRequestAceptedEvent>(_plannedAPCost);
    }

    public int GetPathCostPreview(Cell targetCell)
    {
        return GetPathCost(targetCell);
    }

    protected override void OnMovementStarted()
    {
    }

    protected override void OnMovementFinished()
    {
        if (_plannedPath != null)
            _plannedPath.Clear();

        _plannedAPCost = 0;
        EventBus.Raise<APRefillEvent>();
        _selectedTargetCell = null;
    }

    /*
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || IsMoving)
            return;

        if (_selectedTargetCell == null)
        {
            DrawReachableCells();
        }
        else
        {
            DrawReachableCells();
            DrawPlannedPath();
            DrawSelectedTarget();
        }
    }

    private void DrawReachableCells()
    {
        if (MapGrid == null || APWallet == null)
            return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);

        for (int x = 0; x < MapGrid.Width; x++)
        {
            for (int z = 0; z < MapGrid.Height; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);

                int cost = GetPathCost(cell);

                if (cost <= 0)
                    continue;

                if (cost > APWallet.CurrentAP)
                    continue;

                Vector3 pos = cell.GetWorldTopPosition();
                pos.y += 0.05f;

                Gizmos.DrawCube(pos, Vector3.one * 0.35f);
            }
        }
    }

    private void DrawPlannedPath()
    {
        if (_plannedPath == null)
            return;

        Gizmos.color = Color.blue;

        foreach (Cell cell in _plannedPath)
        {
            Vector3 pos = cell.GetWorldTopPosition();
            pos.y += 0.1f;

            Gizmos.DrawCube(pos, Vector3.one * 0.25f);
        }
    }

    private void DrawSelectedTarget()
    {
        if (_selectedTargetCell == null)
            return;

        Gizmos.color = Color.yellow;

        Vector3 pos = _selectedTargetCell.GetWorldTopPosition();
        pos.y += 0.15f;

        Gizmos.DrawSphere(pos, 0.3f);
    }
    */
}