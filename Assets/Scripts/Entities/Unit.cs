using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : BaseEntity
{
    protected PathFinding PathFinding => ServiceProvider.Instance.GetService<PathFinding>();

    [Header("Spawn")]
    [SerializeField] protected Cell _spawnCell;

    public Cell SpawnCell => _spawnCell;
    public void SetSpawnCell(Cell spawnCell) => _spawnCell = spawnCell;

    [Header("Movement")]
    [SerializeField] protected int _maxTicksPerTurn = 7;
    [SerializeField] protected float _timeToMoveCells = 0.2f;
    [SerializeField] protected float _timeToStayInCell = 0.05f;
    protected List<TurnAction> _plannedActions = new();
    public List<TurnAction> PlannedActions => _plannedActions;

    protected List<Cell> _currentPath;
    protected int _pathIndex;
    protected bool _isTurnPlaying;

    public bool IsTurnPlaying { get => _isTurnPlaying; set => _isTurnPlaying = value; }

    protected int _currentAction;
    public int CurrentAction { get => _currentAction; set => _currentAction = value; }

    protected Cell _currentCell;

    private int _attackRange = 4;
    public int AttackRange => _attackRange;

    private bool _isStun = false;
    public bool IsStun => _isStun;

    public Cell CurrentCell => _currentCell;
    public int MaxTicksPerTurn => _maxTicksPerTurn;

    public virtual void Init()
    {
        Spawn();
    }

    protected virtual void Spawn()
    {
        _currentCell = _spawnCell;

        if (_currentCell != null)
        {
            transform.position = GetStandPosition(_currentCell.GetWorldTopPosition());
            _currentCell.stander = this;
        }
    }

    public void Stun()
    {
        _isStun = true;
    }

    public void Unstun()
    {
        _isStun = false;
    }

    protected bool IsCellAvailable(Cell targetCell)
    {
        if (targetCell == null)
            return false;

        if (!targetCell.IsWalkable)
            return false;

        if (targetCell.isOccupied)
            return false;

        return true;
    }

    protected int GetPathCost(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return -1;
        }

        List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);

        if (path == null)
            return -1;

        return path.Count - 1;
    }

    protected int GetPathCost(Cell originCell, Cell targetCell)
    {
        if (targetCell == originCell)
            return -1;

        if (!IsCellAvailable(targetCell) && targetCell.stander is not Player)
        {
            Debug.LogWarning($"Target cell unavailable: {targetCell}");
            return -1;
        }

        if (!IsCellAvailable(originCell) && originCell.stander is not Player)
        {
            Debug.LogWarning($"Origin cell unavailable: {originCell}");
            return -1;
        }

        List<Cell> path = PathFinding.FindPath(originCell.Coordinates, targetCell.Coordinates);

        if (path == null)
            return -1;

        return path.Count - 1;
    }

    protected List<Cell> GetPathCells(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell) && targetCell.stander is not Player)
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);
    }

    protected List<Cell> GetPathCells(Cell originCell, Cell targetCell)
    {
        if (!IsCellAvailable(targetCell) && targetCell.stander is not Player)
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(originCell.Coordinates, targetCell.Coordinates);
    }

    public IEnumerator MoveTo(Cell targetCell)
    {
        if (targetCell.isOccupied)
        {
            Debug.Log($"Cell {targetCell} is occupied (stander: {targetCell.stander}). Clearing plan.");
            ClearPlan();
            yield break;
        }

        _isTurnPlaying = true;

        Vector3 startPos = transform.position;
        Vector3 flatTarget = new Vector3(targetCell.transform.position.x, startPos.y, targetCell.transform.position.z);
        Vector3 finalTarget;

        if (targetCell.Height != _currentCell.Height)
        {
            finalTarget = GetStandPosition(targetCell.GetWorldTopPosition());
        }
        else
        {
            finalTarget = targetCell.transform.position;
            finalTarget.y = startPos.y;
        }

        Cell previousCell = _currentCell;
        _currentCell = targetCell;
        previousCell.stander = null;
        _currentCell.stander = this;

        float elapsed = 0;

        while (elapsed < _timeToMoveCells)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, flatTarget, elapsed / _timeToMoveCells);
            yield return null;
        }

        elapsed = 0;

        while (elapsed < _timeToMoveCells * .5f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(flatTarget, finalTarget, elapsed / (_timeToMoveCells * 0.5f));

            yield return null;
        }

        transform.position = finalTarget;

        _isTurnPlaying = false;
    }

    public void MoveInstant(Cell targetCell)
    {
        if (_currentCell != null)
            _currentCell.stander = null;

        _currentCell = targetCell;
        _currentCell.stander = this;

        transform.position = GetStandPosition(targetCell.GetWorldTopPosition());
    }

    protected virtual void OnMovementStarted() { }

    protected virtual void OnMovementFinished() { }

    public virtual IEnumerator<Vector2Int> AttackPattern()
    {
        return null;
    }

    protected Vector3 GetStandPosition(Vector3 basePosition)
    {
        return basePosition + new Vector3(0, transform.localScale.y * 0.5f, 0);
    }

    public virtual void ClearPlan()
    {
        Debug.Log($"{gameObject.name} plan cleared");
        _plannedActions.Clear();
    }

    public virtual void ResetActionsCounter()
    {
        _currentAction = 0;
    }

    public virtual void ConsumeAP(TurnAction action)
    {
    }
}
