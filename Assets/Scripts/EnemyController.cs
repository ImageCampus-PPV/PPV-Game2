using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : UnitController
{
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    private PathFinding PathFinding => ServiceProvider.Instance.GetService<PathFinding>();

    [Header("Enemy")]
    [SerializeField] private int _attackRange = 4;

    public void TakeTurn(Cell playerCell)
    {
        if (_isMoving)
            return;

        if (IsInGoodCover(playerCell))
        {
            Debug.Log($"{name} holding position");
            return;
        }

        Cell targetCell = FindBestCell(playerCell);

        if (targetCell == null)
        {
            Debug.Log($"{name} found no valid move");
            return;
        }

        List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);

        if (path == null || path.Count <= 1)
            return;

        //Move one tile per turn
        _currentPath = new List<Cell>()
        {
            path[0],
            path[1]
        };

        _pathIndex = 1;

        StartCoroutine(FollowPath());
    }

    private bool IsInRange(Cell playerCell)
    {
        return GetGridDistance(_currentCell, playerCell) <= _attackRange;
    }

    private bool IsInGoodCover(Cell playerCell)
    {
        if (_currentCell == null)
            return false;

        return HasCoverAgainstPlayer(_currentCell, playerCell) && IsInRange(playerCell);
    }

    private Cell FindBestCell(Cell playerCell)
    {
        Cell bestCoverCell = null;
        float bestCoverDist = float.MaxValue;

        Cell fallbackCell = null;
        float fallbackDist = float.MaxValue;

        int minX = Mathf.Max(0, playerCell.Coordinates.x - _attackRange);
        int maxX = Mathf.Min(MapGrid.Width - 1, playerCell.Coordinates.x + _attackRange);
        int minZ = Mathf.Max(0, playerCell.Coordinates.y - _attackRange);
        int maxZ = Mathf.Min(MapGrid.Height - 1, playerCell.Coordinates.y + _attackRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);

                if (!cell.IsWalkable)
                    continue;

                if (cell.isOccupied)
                    continue;

                int distToPlayer = GetGridDistance(cell, playerCell);

                if (distToPlayer > _attackRange)
                    continue;

                //Make sure enemy can reach the cell
                List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, cell.Coordinates);

                if (path == null || path.Count <= 1)
                    continue;

                if (HasCoverAgainstPlayer(cell, playerCell))
                {
                    float coverScore = GetGridDistance(cell, _currentCell);

                    if (coverScore < bestCoverDist)
                    {
                        bestCoverDist = coverScore;
                        bestCoverCell = cell;
                    }

                    continue;
                }


                if (!IsInRange(playerCell))
                {
                    //Try to stay close to attack range
                    float rangeScore = Mathf.Abs(distToPlayer - _attackRange);

                    if (rangeScore < fallbackDist)
                    {
                        fallbackDist = rangeScore;
                        fallbackCell = cell;
                    }
                }
            }
        }

        //Prefer cover
        if (bestCoverCell != null)
            return bestCoverCell;

        //Otherwise fallback movement
        return fallbackCell;
    }

    private bool HasCoverAgainstPlayer(Cell cell, Cell playerCell)
    {
        Vector3 end = playerCell.GetWorldTopPosition();
        Vector3 direction = end - cell.GetWorldTopPosition();
        Vector3 start = cell.GetWorldTopPosition() + direction.normalized * 0.1f;
        float distance = direction.magnitude;

        if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance))
        {
            Cell hitCell = hit.collider.GetComponentInParent<Cell>();

            if (hitCell != null && hitCell != playerCell && hitCell != cell && !hitCell.IsWalkable)
                return true;
        }

        return false;
    }

    private int GetGridDistance(Cell a, Cell b)
    {
        return Mathf.Abs(a.Coordinates.x - b.Coordinates.x) + Mathf.Abs(a.Coordinates.y - b.Coordinates.y);
    }
}