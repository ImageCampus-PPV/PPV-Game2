using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PathFinding : IService
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int HEIGHT_DIFFERENCE_TOLERANCE = 1;

    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    public bool IsPersistance => true;

    private List<Cell> _openList;
    private List<Cell> _closedList;


    public PathFinding()
    {
        _openList = new List<Cell>();
        _closedList = new List<Cell>();
    }

    public List<Cell> FindPath(Vector2Int startPos, Vector2Int endPos)
    {
        Cell startCell = MapGrid.GetCell(startPos);
        Cell endCell = MapGrid.GetCell(endPos);

        _openList = new List<Cell>() { startCell };
        _closedList = new List<Cell>();

        for (int x = 0; x < MapGrid.Width; x++)
        {
            for (int z = 0; z < MapGrid.Height; z++)
            {
                Cell cell = MapGrid.GetCell(new Vector2Int(x, z));
                cell.gCost = int.MaxValue;
                cell.hCost = 0;
                cell.CalculateFCost();
                cell.cameFromCell = null;
            }
        }

        startCell.gCost = 0;
        startCell.hCost = CalculateHeurisiticDistanceCost(startCell, endCell);
        startCell.CalculateFCost();

        while (_openList.Count > 0)
        {
            Cell currentCell = GetLowestFCostNode(_openList);
            if (currentCell == endCell)
                return CalculatePath(endCell);

            _openList.Remove(currentCell);
            _closedList.Add(currentCell);

            foreach (Cell neighbourCell in GetNeighbourList(currentCell))
            {
                if (_closedList.Contains(neighbourCell))
                    continue;

                if (!neighbourCell.IsWalkable)
                    continue;

                if (neighbourCell.isOccupied && neighbourCell != endCell)
                    continue;

                int heightDiff = neighbourCell.Height - currentCell.Height;
                if (Mathf.Abs(heightDiff) > HEIGHT_DIFFERENCE_TOLERANCE)
                    continue;

                if (heightDiff != 0)
                {
                    bool hasConnector = currentCell.IsHeightConnector || neighbourCell.IsHeightConnector;
                    if (!hasConnector)
                        continue;
                }

                int tentativeGCost = currentCell.gCost + MOVE_STRAIGHT_COST;

                if (tentativeGCost < neighbourCell.gCost)
                {
                    neighbourCell.cameFromCell = currentCell;
                    neighbourCell.gCost = tentativeGCost;
                    neighbourCell.hCost = CalculateHeurisiticDistanceCost(neighbourCell, endCell);
                    neighbourCell.CalculateFCost();

                    if (!_openList.Contains(neighbourCell))
                        _openList.Add(neighbourCell);
                }
            }
        }

        //No path found and no cells left to check
        return null;
    }


    private List<Cell> GetNeighbourList(Cell cell)
    {
        List<Cell> neighbourList = new List<Cell>();

        int x = cell.Coordinates.x;
        int y = cell.Coordinates.y;

        bool oddRow = y % 2 == 1;

        // Left
        if (x - 1 >= 0)
            neighbourList.Add(MapGrid.GetCell(x - 1, y));

        // Right
        if (x + 1 < MapGrid.Width)
            neighbourList.Add(MapGrid.GetCell(x + 1, y));

        // Down
        if (y - 1 >= 0)
            neighbourList.Add(MapGrid.GetCell(x, y - 1));

        // Up
        if (y + 1 < MapGrid.Height)
            neighbourList.Add(MapGrid.GetCell(x, y + 1));

        //if (oddRow)
        //{
        //    // Up Right
        //    if (y + 1 < MapGrid.Height && x + 1 < MapGrid.Width)
        //        neighbourList.Add(MapGrid.GetCell(x + 1, y + 1));

        //    // Down Right
        //    if (y - 1 >= 0 && x + 1 < MapGrid.Width)
        //        neighbourList.Add(MapGrid.GetCell(x + 1, y - 1));
        //}
        //else
        //{
        //    // Up Left
        //    if (y + 1 < MapGrid.Height && x - 1 >= 0)
        //        neighbourList.Add(MapGrid.GetCell(x - 1, y + 1));

        //    // Down Left
        //    if (y - 1 >= 0 && x - 1 >= 0)
        //        neighbourList.Add(MapGrid.GetCell(x - 1, y - 1));
        //}

        return neighbourList;
    }

    private List<Cell> CalculatePath(Cell endCell)
    {
        List<Cell> path = new List<Cell>();
        path.Add(endCell);
        Cell currentCell = endCell;

        while (currentCell.cameFromCell != null)
        {
            path.Add(currentCell.cameFromCell);
            currentCell = currentCell.cameFromCell;
        }

        path.Reverse();

        //for (int i = 0; i < path.Count; i++)
        //{
        //    Debug.Log($"Step {i + 1}: {path[i].name}");
        //}

        return path;
    }

    private Cell GetLowestFCostNode(List<Cell> cellsList)
    {
        Cell lowestFCostCell = cellsList[0];

        for (int i = 1; i < cellsList.Count; i++)
            if (cellsList[i].fCost < lowestFCostCell.fCost)
                lowestFCostCell = cellsList[i];

        return lowestFCostCell;
    }

    private int CalculateHeurisiticDistanceCost(Cell a, Cell b)
    {
        return Mathf.RoundToInt(
            MOVE_STRAIGHT_COST * Vector3.Distance(MapGrid.GetWorldPosition(a.Coordinates.x, a.Coordinates.y), MapGrid.GetWorldPosition(b.Coordinates.x, b.Coordinates.y)));
    }
}
