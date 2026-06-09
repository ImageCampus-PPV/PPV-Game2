using ImageCampus.ToolBox.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    protected PathFinding PathFinding => ServiceProvider.Instance.GetService<PathFinding>();

    public const uint NULL_UNIT = 0;

    private uint _unitID = NULL_UNIT;

    public uint ID => _unitID;

    [Header("Spawn")]
    [SerializeField] protected Cell _spawnCell;

    [Header("Movement")]
    [SerializeField] protected float _timeToMoveCells = 0.2f;
    [SerializeField] protected float _timeToStayInCell = 0.05f;

    protected List<Cell> _currentPath;
    protected int _pathIndex;
    protected bool _isMoving;

    protected Cell _currentCell;

    public Cell CurrentCell => _currentCell;
    public bool IsMoving => _isMoving;

    protected virtual void Start()
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

    public void SetID(uint id)
    {
        _unitID = id;
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

    protected List<Cell> GetPathCells(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);
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

    protected virtual void OnMovementStarted() { }

    protected virtual void OnMovementFinished() { }

    protected Vector3 GetStandPosition(Vector3 basePosition)
    {
        return basePosition + new Vector3(0, transform.localScale.y * 0.5f, 0);
    }
}
