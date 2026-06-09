using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : Unit
{
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    private uint _damage = 10;
    public uint Damage => _damage;
    private int _movementRange = 4;

    public void TakeTurn(Cell playerCell)
    {
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
        return GetGridDistance(_currentCell, playerCell) <= _movementRange;
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

        int minX = Mathf.Max(0, playerCell.Coordinates.x - _movementRange);
        int maxX = Mathf.Min(MapGrid.Width - 1, playerCell.Coordinates.x + _movementRange);
        int minZ = Mathf.Max(0, playerCell.Coordinates.y - _movementRange);
        int maxZ = Mathf.Min(MapGrid.Height - 1, playerCell.Coordinates.y + _movementRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);

                if (cell == null)
                    continue;

                if (!cell.IsWalkable)
                    continue;

                if (cell.isOccupied)
                    continue;

                int distToPlayer = GetGridDistance(cell, playerCell);

                if (distToPlayer > _movementRange)
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
                    float rangeScore = Mathf.Abs(distToPlayer - _movementRange);

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