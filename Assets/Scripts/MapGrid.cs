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

    //This should be optimize.
    private CellsMaps _cellMap;

    private Cell[,] _gridArray;
    [SerializeField] private GameObject _cellPrefab;

    public MapGrid(CellsMaps cellsMaps)
    {
        _cellMap = cellsMaps;
    }

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
        _cellsX = _cellMap.size.x;
        _cellsZ = _cellMap.size.y;

        Dictionary<string, Type> cellStatesPerName = new Dictionary<string, Type>();

        foreach (Type type in GetType().Assembly.GetTypes())
        {
            if (typeof(State).IsAssignableFrom(type) && type.GetCustomAttribute<CellStateAttribute>() != null)
                cellStatesPerName.Add(type.Name, type);
        }

        _gridArray = new Cell[_cellMap.size.x, _cellMap.size.y];

        foreach (CellData cell in _cellMap._cellsData)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(cell._coordinates.x * 1.25f, 0.0f, cell._coordinates.y * 1.25f);

            Cell cellObject = go.AddComponent<Cell>();

            _gridArray[cell._coordinates.x, cell._coordinates.y] = cellObject;

            cellObject.SetCoordinate(cell._coordinates);
            cellObject.Init(cellStatesPerName[cell._initialState]);
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