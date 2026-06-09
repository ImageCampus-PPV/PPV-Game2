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

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    ReactToInput(clickedCell);
        }

        if(Input.GetKeyUp(KeyCode.Space))
            _isTurnReady = true;
    }

    private void ReactToInput(Cell clickedCell)
    {
        if (_selectedTargetCell == clickedCell && clickedCell != _currentCell)
        {
            _isTurnReady = true;
            return;
        }


        if (IsCellAvailable(clickedCell))
        {
            if (GetPathCost(clickedCell) <= APWallet.CurrentAP)
            {
                _plannedAPCost = GetPathCost(clickedCell);
                EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
                _plannedPath = GetPathCells(clickedCell);
                _selectedTargetCell = clickedCell;
            }
        }
        else if(clickedCell == _currentCell)
        {
            _plannedAPCost = 0;
            EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
            _plannedPath.Clear();
            _selectedTargetCell = clickedCell;
        }
        else
            _selectedTargetCell = null;

    }


    public void HandleMovement()
    {
        _isTurnReady = false;

        if (_selectedTargetCell == null)
            return;

        if (_isMoving)
            return;

        Debug.Log("HANDLE MOVEMENT CALLED");
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
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP - _plannedAPCost, APWallet.MaxAP);
        _selectedTargetCell = null;
    }
}