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
    [SerializeField] protected float _timeToMoveCells = 0.2f;
    [SerializeField] protected float _timeToStayInCell = 0.05f;

    protected List<Cell> _currentPath;
    protected int _pathIndex;
    protected bool _isMoving;

    protected Cell _currentCell;

    private int _attackRange = 4;
    public int AttackRange => _attackRange;

    private bool _isStun = false;
    public bool IsStun => _isStun;

    public Cell CurrentCell => _currentCell;
    public bool IsMoving => _isMoving;

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

    protected void RequestPath(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return;
        }

        Debug.Log($"Target: {targetCell}");
        Debug.Log($"CurrentCell: {_currentCell}");
        Debug.Log($"PathfindingController: {PathFinding}");

        List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);

        if (path != null && path.Count > 1)
        {
            _currentPath = path;
            _pathIndex = 1;

            _isMoving = true;
            StartCoroutine(FollowPath());
        }
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
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return -1;
        }

        List<Cell> path = PathFinding.FindPath(originCell.Coordinates, targetCell.Coordinates);

        if (path == null)
            return -1;

        return path.Count - 1;
    }

    protected List<Cell> GetPathCells(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);
    }

    protected List<Cell> GetPathCells(Cell originCell, Cell targetCell)
    {
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(originCell.Coordinates, targetCell.Coordinates);
    }

    protected IEnumerator FollowPath()
    {
        while (_pathIndex < _currentPath.Count)
        {
            Cell targetCell = _currentPath[_pathIndex];

            if (targetCell.isOccupied)
                break;

            Vector3 startPos = transform.position;

            Vector3 flatTarget = new Vector3(targetCell.transform.position.x, startPos.y, targetCell.transform.position.z);
            Vector3 finalTarget;

            if (targetCell.Height != _currentCell.Height)
                finalTarget = GetStandPosition(targetCell.GetWorldTopPosition());
            else
            {
                finalTarget = targetCell.transform.position;
                finalTarget.y = transform.position.y;
            }

            Cell previousCell = _currentCell;
            _currentCell = targetCell;
            previousCell.stander = null;
            _currentCell.stander = this;
            OnMovementStarted();

            float elapsed = 0f;
            while (elapsed < _timeToMoveCells)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _timeToMoveCells;

                transform.position = Vector3.Lerp(startPos, flatTarget, t);
                yield return null;
            }

            elapsed = 0f;
            float heightTime = _timeToMoveCells * 0.5f;

            while (elapsed < heightTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / heightTime;

                transform.position = Vector3.Lerp(flatTarget, finalTarget, t);
                yield return null;
            }

            transform.position = finalTarget;

            _pathIndex++;

            if (_timeToStayInCell > 0)
                yield return new WaitForSeconds(_timeToStayInCell);
        }

        _isMoving = false;
        OnMovementFinished();
    }

    protected IEnumerator FollowPath(List<Cell> path)
    {
        _currentPath = new List<Cell>(path);
        _pathIndex = 0;
        _isMoving = true;

        while (_pathIndex < _currentPath.Count)
        {
            Cell targetCell = _currentPath[_pathIndex];

            if (targetCell.isOccupied)
                break;

            Vector3 startPos = transform.position;

            Vector3 flatTarget = new Vector3(targetCell.transform.position.x, startPos.y, targetCell.transform.position.z);
            Vector3 finalTarget;

            if (targetCell.Height != _currentCell.Height)
                finalTarget = GetStandPosition(targetCell.GetWorldTopPosition());
            else
            {
                finalTarget = targetCell.transform.position;
                finalTarget.y = transform.position.y;
            }

            Cell previousCell = _currentCell;
            _currentCell = targetCell;
            previousCell.stander = null;
            _currentCell.stander = this;
            OnMovementStarted();

            float elapsed = 0f;
            while (elapsed < _timeToMoveCells)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _timeToMoveCells;

                transform.position = Vector3.Lerp(startPos, flatTarget, t);
                yield return null;
            }

            elapsed = 0f;
            float heightTime = _timeToMoveCells * 0.5f;

            while (elapsed < heightTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / heightTime;

                transform.position = Vector3.Lerp(flatTarget, finalTarget, t);
                yield return null;
            }

            transform.position = finalTarget;

            _pathIndex++;

            if (_timeToStayInCell > 0)
                yield return new WaitForSeconds(_timeToStayInCell);
        }

        _isMoving = false;
        OnMovementFinished();
    }

    public IEnumerator MoveTo(Cell targetCell)
    {
        _isMoving = true;

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

        Cell previous = _currentCell;
        previous.stander = null;

        _currentCell = targetCell;
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

        _isMoving = false;
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
}
