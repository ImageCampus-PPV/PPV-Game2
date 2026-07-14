using Assets.Scripts;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
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
    private Floor _cellMap;

    private GameObject _playerPrefab;
    private GameObject _heavyEngine;
    private GameObject _lightEnemy;
    private GameObject _normalEnemy;

    private Cell[,] _gridArray;
    [SerializeField] private GameObject _cellPrefab;

    private Dictionary<string, Type> _cellStatesPerName = new Dictionary<string, Type>();

    public MapGrid(GameObject playerPrefab, GameObject heavyEngine, GameObject lightEnemy, GameObject normalEnemy, Floor cellsMaps)
    {
        _playerPrefab = playerPrefab;
        _heavyEngine = heavyEngine;
        _lightEnemy = lightEnemy;
        _normalEnemy = normalEnemy;
        _cellMap = cellsMaps;
    }

    public void Init()
    {
        EventBus.Subscribe<InfectTilesEvent>(OnTileContagiousSpread);
        EventBus.Subscribe<TurnTileHealing>(OnTileTurnHeal);
        EventBus.Subscribe<TurnsTileContagious>(OnTurnsTileContagious);
        EventBus.Subscribe<TurnTileIntoUnstable>(OnTurnTileIntoUnstable);
        EventBus.Subscribe<TurnTileBroken>(OnTurnTileBroken);
        EventBus.Subscribe<DevChangeCellStateEvent>(OnDevChangeCellState);
        EventBus.Subscribe<DevSpawnEnemyEvent>(OnDevSpawnEnemy);
        EventBus.Subscribe<DevRemoveEntityAtCellEvent>(OnDevRemoveEntity);

        Build();
    }

    private void OnDevChangeCellState(in DevChangeCellStateEvent e)
    {
        if (e.coordX < 0 || e.coordX >= Width || e.coordY < 0 || e.coordY >= Height) return;
        if (!_cellStatesPerName.ContainsKey(e.stateName)) return;
        GetCell(e.coordX, e.coordY).Transition(_cellStatesPerName[e.stateName]);
    }

    private void OnDevSpawnEnemy(in DevSpawnEnemyEvent e)
    {
        Cell cell = (e.coordX < 0 || e.coordX >= Width || e.coordY < 0 || e.coordY >= Height)
            ? null : GetCell(e.coordX, e.coordY);

        if (cell == null || cell.isOccupied || !cell.IsWalkable) 
            return;

        GameObject prefab = e.enemyTypeName switch
        {
            nameof(HeavyEnemy) => _heavyEngine,
            nameof(LightEnemy) => _lightEnemy,
            nameof(NormalEnemy) => _normalEnemy,
            _ => null
        };

        if (prefab == null)
            return;

        GameObject go = UnityEngine.Object.Instantiate(prefab);

        Unit unit = e.enemyTypeName switch
        {
            nameof(HeavyEnemy) => go.AddComponent<HeavyEnemy>(),
            nameof(LightEnemy) => go.AddComponent<LightEnemy>(),
            nameof(NormalEnemy) => go.AddComponent<NormalEnemy>(),
            _ => null
        };

        unit.SetSpawnCell(cell);
        unit.Init();

        ServiceProvider.Instance.GetService<EntityRegistry>().Add(unit);
    }

    private void OnDevRemoveEntity(in DevRemoveEntityAtCellEvent e)
    {
        if (e.coordX < 0 || e.coordX >= Width || e.coordY < 0 || e.coordY >= Height) 
            return;

        Cell cell = GetCell(e.coordX, e.coordY);
        Unit unit = cell?.stander;

        if (unit == null) 
            return;

        if (unit is Player) 
            return;

        EntityRegistry registry = ServiceProvider.Instance.GetService<EntityRegistry>();
        cell.stander = null;
        registry.Remove(unit);
        UnityEngine.Object.Destroy(unit.gameObject);
    }

    private void Build()
    {
        _cellsX = _cellMap.size.x;
        _cellsZ = _cellMap.size.y;

        GameObject goPlayer = UnityEngine.Object.Instantiate(_playerPrefab);

        Player player = goPlayer.AddComponent<Player>();

        _cellStatesPerName.Clear();

        foreach (Type type in GetType().Assembly.GetTypes())
        {
            if (typeof(State).IsAssignableFrom(type) && type.GetCustomAttribute<CellStateAttribute>() != null)
                _cellStatesPerName.Add(type.Name, type);
        }

        Dictionary<string, Type> enemiesType = new Dictionary<string, Type>();

        GameObject goEnemy = null;
        Unit goEnemyScript = null;

        _gridArray = new Cell[_cellMap.size.x, _cellMap.size.y];

        foreach (CellData cell in _cellMap._cellsData)
        {
            GameObject goCell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goCell.transform.position = new Vector3(cell._coordinates.x * 1.25f, 0.0f, cell._coordinates.y * 1.25f);

            Cell cellObject = goCell.AddComponent<Cell>();
            goCell.layer = 6;

            _gridArray[cell._coordinates.x, cell._coordinates.y] = cellObject;

            cellObject.SetCoordinate(cell._coordinates);
            cellObject.Init(_cellStatesPerName[cell._initialState]);

            switch (cell._spawnUnit)
            {
                case nameof(Player):
                    player.SetSpawnCell(cellObject);
                    break;

                case nameof(HeavyEnemy):
                    goEnemy = UnityEngine.Object.Instantiate(_heavyEngine);
                    goEnemyScript = goEnemy.AddComponent<HeavyEnemy>();
                    break;

                case nameof(LightEnemy):
                    goEnemy = UnityEngine.Object.Instantiate(_lightEnemy);
                    goEnemyScript = goEnemy.AddComponent<LightEnemy>();
                    break;

                case nameof(NormalEnemy):
                    goEnemy = UnityEngine.Object.Instantiate(_normalEnemy);
                    goEnemyScript = goEnemy.AddComponent<NormalEnemy>();
                    break;

                default:
                    break;
            }

            if (goEnemyScript != null)
            {
                goEnemyScript.SetSpawnCell(cellObject);
                goEnemyScript.Init();
            }

            goEnemyScript = null;
            goEnemy = null;
        }

        player.Init();

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