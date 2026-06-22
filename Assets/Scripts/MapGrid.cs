using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using EventBus = ImageCampus.ToolBox.Events.EventBus;

public class MapGrid : IService, IDisposable
{
    public bool IsPersistance => false;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    [SerializeField] private int _cellsX;
    [SerializeField] private int _cellsZ;
    public int Width => _cellsX;
    public int Height => _cellsZ;

    private Cell[,] _gridArray;
    [SerializeField] private GameObject _cellPrefab;

    public void Init()
    {
        EventBus.Subscribe<InfectTilesEvent>(OnTileContagiousSpread);
        EventBus.Subscribe<TurnTileHealing>(OnTileTurnHeal);
        EventBus.Subscribe<TurnsTileContagious>(OnTurnsTileContagious);
        EventBus.Subscribe<TurnTileIntoUnstable>(OnTurnTileIntoUnstable);
        EventBus.Subscribe<TurnTileBroken>(OnTurnTileBroken);

        Build();
    }

    private void Build()
    {
        Dictionary<string, Type> cellTypesPerName = new Dictionary<string, Type>();

        foreach (Type type in GetType().Assembly.GetTypes())
        {
            if (type.GetAttribute<CellStateAttribute>() == null)
                continue;

            cellTypesPerName.Add(type.Name, type);
        }


        Cell[] cells = UnityEngine.Object.FindObjectsOfType<Cell>();

        if (cells.Length == 0)
            return;

        (int minX, int minZ, int maxX, int maxZ) = GetMinMaxSize();

        _cellsX = maxX - minX + 1;
        _cellsZ = maxZ - minZ + 1;

        _gridArray = new Cell[_cellsX, _cellsZ];

        foreach (Cell cell in cells)
        {
            Vector2Int coord = cell.Coordinates;

            int x = coord.x - minX;
            int z = coord.y - minZ;

            _gridArray[x, z] = cell;

            cell.Init(cellTypesPerName[cell.InitialState]);
        }

        (int minX, int minZ, int maxX, int maxZ) GetMinMaxSize()
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;

            foreach (Cell cell in cells)
            {
                Vector2Int coord = cell.Coordinates;

                if (coord.x < minX) minX = coord.x;
                if (coord.x > maxX) maxX = coord.x;

                if (coord.y < minZ) minZ = coord.y;
                if (coord.y > maxZ) maxZ = coord.y;
            }

            return (minX, minZ, maxX, maxZ);
        }
    }

    public void Tick(float deltaTime)
    {
        foreach (Cell cell in _gridArray)
            cell.Tick(deltaTime);
    }

    public Cell GetCell(Vector2Int coordinates)
    {
        return _gridArray[coordinates.x, coordinates.y];
    }

    public Cell GetCell(int x, int z)
    {
        return _gridArray[x, z];
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return _gridArray[x, z].transform.position;
        //return
        //    new Vector3(x, 0, 0) * _cellsSize +
        //    new Vector3(0, 0, z) * _cellsSize * VERTICAL_OFFSET_MULTIPLIER +
        //    ((Mathf.Abs(z) % 2) == 1 ? new Vector3(1, 0, 0) * _cellsSize * .5f : Vector3.zero);
    }

    private void OnTileContagiousSpread(in InfectTilesEvent infectTilesEvent)
    {
        Vector2Int[] directions =
       {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };


        Vector2Int posToCheck;

        foreach (Vector2Int dir in directions)
        {
            posToCheck = infectTilesEvent.position + dir;

            Cell neighbor = posToCheck.x >=
                Width || posToCheck.x < 0 || posToCheck.y >= Height || posToCheck.y < 0 ?
                null :
                GetCell(infectTilesEvent.position + dir);

            if (neighbor != null && neighbor.IsWalkable && neighbor.GetState() != typeof(Infected) && neighbor.GetState() != typeof(Contagious))
                neighbor.Transition(typeof(Contagious));
        }
    }

    private void OnTileTurnHeal(in TurnTileHealing turnTileHealing)
    {
        Vector2Int posToCheck = new Vector2Int(turnTileHealing.coordX, turnTileHealing.coordY);
        Cell cell = posToCheck.x >=
             Width || posToCheck.x < 0 || posToCheck.y >= Height || posToCheck.y < 0 ?
             null :
             GetCell(posToCheck);

        if (cell)
            cell.Transition(typeof(Healing));
    }

    private void OnTurnsTileContagious(in TurnsTileContagious turnsTileContagious)
    {
        Vector2Int posToCheck = new Vector2Int(turnsTileContagious.coordX, turnsTileContagious.coordY);
        Cell cell = posToCheck.x >=
             Width || posToCheck.x < 0 || posToCheck.y >= Height || posToCheck.y < 0 ?
             null :
             GetCell(posToCheck);

        if (cell)
            cell.Transition(typeof(Contagious));
    }

    private void OnTurnTileIntoUnstable(in TurnTileIntoUnstable turnTileIntoUnstable)
    {
        Vector2Int posToCheck = new Vector2Int(turnTileIntoUnstable.coordX, turnTileIntoUnstable.coordY);
        Cell cell = posToCheck.x >=
             Width || posToCheck.x < 0 || posToCheck.y >= Height || posToCheck.y < 0 ?
             null :
             GetCell(posToCheck);

        if (cell)
            cell.Transition(typeof(Unstable));
    }
    private void OnTurnTileBroken(in TurnTileBroken turnTileIntoUnstable)
    {
        Vector2Int posToCheck = new Vector2Int(turnTileIntoUnstable.coordX, turnTileIntoUnstable.coordY);
        Cell cell = posToCheck.x >=
             Width || posToCheck.x < 0 || posToCheck.y >= Height || posToCheck.y < 0 ?
             null :
             GetCell(posToCheck);

        if (cell)
            cell.Transition(typeof(Broken));
    }

    public void Dispose()
    {
        EventBus.Unsubscribe<InfectTilesEvent>(OnTileContagiousSpread);
    }
}

public struct TurnTileHealing : IEvent
{
    public int coordX;
    public int coordY;

    public void Assign(params object[] parameters)
    {
        coordX = (int)parameters[0];
        coordY = (int)parameters[1];
    }

    public void Reset()
    {
        coordX = default(int);
        coordY = default(int);
    }
}

public struct TurnsTileContagious : IEvent
{
    public int coordX;
    public int coordY;

    public void Assign(params object[] parameters)
    {
        coordX = (int)parameters[0];
        coordY = (int)parameters[1];
    }

    public void Reset()
    {
        coordX = default(int);
        coordY = default(int);
    }
}

public struct TurnTileIntoUnstable : IEvent
{
    public int coordX;
    public int coordY;

    public void Assign(params object[] parameters)
    {
        coordX = (int)parameters[0];
        coordY = (int)parameters[1];
    }

    public void Reset()
    {
        coordX = default(int);
        coordY = default(int);
    }
}

public struct TurnTileBroken : IEvent
{
    public int coordX;
    public int coordY;

    public void Assign(params object[] parameters)
    {
        coordX = (int)parameters[0];
        coordY = (int)parameters[1];
    }

    public void Reset()
    {
        coordX = default(int);
        coordY = default(int);
    }
}