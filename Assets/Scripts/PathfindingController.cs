using System.Collections.Generic;
using UnityEngine;

public class PathfindingController : MonoBehaviour
{
    [SerializeField] private MyGrid _grid;
    private Pathfinding _pathfinding;

    private void Awake()
    {
        _pathfinding = new Pathfinding(_grid);
    }

    public List<Cell> FindPath(Vector2Int start, Vector2Int end)
    {
        return _pathfinding.FindPath(start, end);
    }
}