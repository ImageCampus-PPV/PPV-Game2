using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : Unit
{
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    //Normal enemy values
    protected uint _damage = 25;
    protected int _movementRange = 1;
    protected int _fortitude = 2;
    protected int _pushDistance = 2;
    protected int _attackTickCost = 1;

    public uint Damage => _damage;
    public int MovementRange => _movementRange;
    public int Fortitude => _fortitude;
    public int PushDistance => _pushDistance;
    public int AttackTickCost => _attackTickCost;


    public void PlanTurn(Cell playerCell, int emptyActions)
    {
        _plannedActions.Clear();
        _usedTicksThisTurn = 0;

        for (int i = 0; i < emptyActions; i++)
            _plannedActions.Add(new WaitAction());

        Cell newCurrentCell = _currentCell;
        if (!IsInAttackRange(_currentCell, playerCell))
            newCurrentCell = PlanMovementActions(playerCell);

        if (IsInAttackRange(newCurrentCell, playerCell))
            PlanCombatActions(playerCell);


        //Move one tile per turn
        //_currentPath = new List<Cell>()
        //{
        //    path[0],
        //    path[1]
        //};
        //
        //_pathIndex = 1;
        //
        //StartCoroutine(FollowPath());
    }

    private Cell PlanMovementActions(Cell playerCell)
    {
        Cell newCurrentCell = CurrentCell;
        //if (IsInGoodCover(playerCell))
        //{
        //    Debug.Log($"{name} holding position");
        //    return;
        //}

        Cell targetCell = FindBestCell(playerCell);

        if (targetCell == null)
        {
            Debug.Log($"{name} found no valid move");
            return newCurrentCell;
        }

        List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);

        if (path == null || path.Count <= 1)
            return newCurrentCell;

        int actionLimit = path.Count < _maxTicksPerTurn ? path.Count : _maxTicksPerTurn;

        for (int i = 1; i < actionLimit; i++)
        {
            _plannedActions.Add(new MoveAction(path[i - 1], path[i], 0));
            newCurrentCell = path[i];
        }

        return newCurrentCell;
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

    protected bool IsInAttackRange(Cell originCell, Cell targetCell)
    {
        Vector2Int origin = originCell.Coordinates;
        Vector2Int target = targetCell.Coordinates;

        Vector2Int[] directions = new Vector2Int[]
        {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        };

        foreach (Vector2Int dir in directions)
        {
            for (int i = 1; i <= AttackRange; i++)
            {
                Vector2Int current = origin + dir * i;

                if (current.x < 0 || current.y < 0 || current.x >= MapGrid.Width || current.y >= MapGrid.Height)
                    break;

                Cell currentCell = MapGrid.GetCell(current);

                if (currentCell.ProvidesCover)
                    break;

                if (current == target)
                    return true;
            }
        }

        return false;
    }

    private int GetGridDistance(Cell a, Cell b)
    {
        return Mathf.Abs(a.Coordinates.x - b.Coordinates.x) + Mathf.Abs(a.Coordinates.y - b.Coordinates.y);
    }

    protected virtual void PlanCombatActions(Cell playerCell)
    {
    }
}