using Assets.Scripts;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using EventBus = ImageCampus.ToolBox.Events.EventBus;

public class MapGrid : IService, IDisposable
{
    public bool IsPersistance => false;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    [SerializeField] private int _cellsX;
    [SerializeField] private int _cellsZ;
    [SerializeField] private GameObject _cellPrefab;

    public int Width => _cellsX;
    public int Height => _cellsZ;

    //This should be optimize.
    private Floor _cellMap;
    private GameObject _playerPrefab;
    private GameObject _heavyEngine;
    private GameObject _lightEnemy;
    private GameObject _normalEnemy;
    private Cell[,] _gridArray;
    private TerminalConfiguration _terminalConfiguration;
 
    private Dictionary<string, Type> _cellStatesPerName = new Dictionary<string, Type>();
    
    public MapGrid(GameObject playerPrefab, GameObject heavyEngine, GameObject lightEnemy, GameObject normalEnemy, Floor cellsMaps, TerminalConfiguration terminalConfiguration = null)
    {
        _playerPrefab = playerPrefab;
        _heavyEngine = heavyEngine;
        _lightEnemy = lightEnemy;
        _normalEnemy = normalEnemy;
        _cellMap = cellsMaps;
        _terminalConfiguration = terminalConfiguration;
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
        EventBus.Subscribe<DevResizeGridEvent>(OnDevResizeGrid);

        Build();
    }

    private void OnDevResizeGrid(in DevResizeGridEvent resizeGridEvent)
    {
        int newWidth = Mathf.Max(1, resizeGridEvent.width);
        int newHeight = Mathf.Max(1, resizeGridEvent.height);

        EntityRegistry registry = ServiceProvider.Instance.GetService<EntityRegistry>();
        Player player = registry.FilterEntities<Player>().First();

        if (player == null)
        {
            Debug.LogError("Cannot find player in entity registry.");
            return;
        }

        if (player.CurrentCell == null)
        {
            Debug.LogError("Player's current cell is null");
            return;
        }

        Vector2Int playerCoord = player.CurrentCell.Coordinates;

        List<(Type enemyType, Vector2Int coord)> survivors = new();

        List<Enemy> enemies = new();

        foreach (Enemy enemy in registry.FilterEntities<Enemy>())
            enemies.Add(enemy);

        foreach (Enemy enemy in enemies)
        {
            Vector2Int coord = enemy.CurrentCell.Coordinates;

            if (coord.x < newWidth && coord.y < newHeight)
                survivors.Add((enemy.GetType(), coord));

            registry.Remove(enemy);
            UnityEngine.Object.Destroy(enemy.gameObject);
        }

        foreach (Cell cell in _gridArray)
            if (cell != null)
                UnityEngine.Object.Destroy(cell.gameObject);

        _cellsX = newWidth;
        _cellsZ = newHeight;
        _gridArray = new Cell[_cellsX, _cellsZ];

        Type defaultState = _cellStatesPerName.Values.First();

        for (int x = 0; x < _cellsX; x++)
        {
            for (int z = 0; z < _cellsZ; z++)
            {
                GameObject goCell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                goCell.transform.position = new Vector3(x * 1.25f, 0f, z * 1.25f);
                goCell.layer = 6;

                Cell cellObject = goCell.AddComponent<Cell>();
                cellObject.SetCoordinate(new Vector2Int(x, z));
                cellObject.Init(defaultState);

                _gridArray[x, z] = cellObject;
            }
        }

        Vector2Int clamped = new Vector2Int(
            Mathf.Clamp(playerCoord.x, 0, _cellsX - 1),
            Mathf.Clamp(playerCoord.y, 0, _cellsZ - 1));

        player.MoveInstant(GetCell(clamped));

        foreach ((Type enemyType, Vector2Int coord) in survivors)
        {
            GameObject prefab = enemyType == typeof(HeavyEnemy) ? _heavyEngine
                : enemyType == typeof(LightEnemy) ? _lightEnemy
                : enemyType == typeof(NormalEnemy) ? _normalEnemy
                : null;

            if (prefab == null) 
                continue;

            GameObject go = UnityEngine.Object.Instantiate(prefab);
            Unit unit = (Unit)go.AddComponent(enemyType);
            unit.SetSpawnCell(GetCell(coord));
            unit.Init();
            registry.Add(unit);
        }
    }

    private void OnDevChangeCellState(in DevChangeCellStateEvent changeCellEvent)
    {
        if (changeCellEvent.coordX < 0 || changeCellEvent.coordX >= Width || changeCellEvent.coordY < 0 || changeCellEvent.coordY >= Height) 
            return;

        if (!_cellStatesPerName.ContainsKey(changeCellEvent.stateName)) 
            return;

        GetCell(changeCellEvent.coordX, changeCellEvent.coordY).Transition(_cellStatesPerName[changeCellEvent.stateName]);
    }

    private void OnDevSpawnEnemy(in DevSpawnEnemyEvent spawnEnemyEvent)
    {
        Cell cell = (spawnEnemyEvent.coordX < 0 || spawnEnemyEvent.coordX >= Width || spawnEnemyEvent.coordY < 0 || spawnEnemyEvent.coordY >= Height)
            ? null : GetCell(spawnEnemyEvent.coordX, spawnEnemyEvent.coordY);

        if (cell == null || cell.isOccupied || !cell.IsWalkable) 
            return;

        GameObject prefab = spawnEnemyEvent.enemyTypeName switch
        {
            nameof(HeavyEnemy) => _heavyEngine,
            nameof(LightEnemy) => _lightEnemy,
            nameof(NormalEnemy) => _normalEnemy,
            _ => null
        };

        if (prefab == null)
            return;

        GameObject go = UnityEngine.Object.Instantiate(prefab);

        Unit unit = spawnEnemyEvent.enemyTypeName switch
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

    private void OnDevRemoveEntity(in DevRemoveEntityAtCellEvent removeEntityEvent)
    {
        if (removeEntityEvent.coordX < 0 || removeEntityEvent.coordX >= Width || removeEntityEvent.coordY < 0 || removeEntityEvent.coordY >= Height) 
            return;

        Cell cell = GetCell(removeEntityEvent.coordX, removeEntityEvent.coordY);
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

            if (!string.IsNullOrEmpty(cell._terminalType) && cell._terminalType != "None")
            {
                if (Enum.TryParse(cell._terminalType, out TerminalType terminalType))
                {
                    Terminal terminal = goCell.AddComponent<Terminal>();
                    terminal.SetType(terminalType);
                    terminal.Init(cellObject, _terminalConfiguration);
                    cellObject.Terminal = terminal;
                }
                else
                {
                    Debug.LogWarning($"[MapGrid] Tipo de terminal desconocido '{cell._terminalType}' en la celda {cell._coordinates}.");
                }
            }

            if (cell._assetToSpawn != null)
            {
                GameObject decoration = UnityEngine.Object.Instantiate(cell._assetToSpawn, goCell.transform);

                float cellTopY = 0.5f * goCell.transform.localScale.y;

                Renderer[] renderers = decoration.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);

                    float pivotToBottom = decoration.transform.position.y - bounds.min.y;
                    decoration.transform.localPosition = new Vector3(0f, cellTopY + pivotToBottom, 0f);
                }
                else
                {
                    decoration.transform.localPosition = new Vector3(0f, cellTopY, 0f);
                }
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