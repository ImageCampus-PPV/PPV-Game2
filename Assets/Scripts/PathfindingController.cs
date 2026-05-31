using System.Collections.Generic;
using UnityEngine;

public class PathfindingController : MonoBehaviour
{
    private Pathfinding _pathfinding;

    private void Awake()
    {
        _pathfinding = new Pathfinding();
    }

    public List<Cell> FindPath(Vector2Int start, Vector2Int end)
    {
        return _pathfinding.FindPath(start, end);
    }
}