using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using EventBus = ImageCampus.ToolBox.Events.EventBus;

public class MapGrid : IService, IDisposable
{
    private const float CELL_SIZE = 1.5f;
    private const int CELL_LAYER = 6;

    private EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    public bool IsPersistance => false;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private int _cellsX;
    private int _cellsZ;
    private Material _defaultMat;

    public int Width => _cellsX;
    public int Height => _cellsZ;

    //This should be optimize.
    private Floor _cellMap;
    private GameObject _playerPrefab;
    private GameObject _heavyEnemy;
    private GameObject _lightEnemy;
    private GameObject _normalEnemy;
    private Cell[,] _gridArray;
    private TerminalConfiguration _terminalConfiguration;

    private Dictionary<string, Type> _cellStatesPerName = new Dictionary<string, Type>();

    private Dictionary<Type, List<Vector2Int>> _cellTypePerCoords;

    [SerializeField] private GameObject _cellPrefab;

    private Grid _grid;

    public MapGrid(Grid grid, GameObject playerPrefab, GameObject heavyEnemy, GameObject lightEnemy, GameObject normalEnemy, Floor cellsMaps, TerminalConfiguration terminalConfiguration, Material defaultMat)
    {
        _grid = grid;

        _grid.cellSwizzle = GridLayout.CellSwizzle.XZY;
        _grid.cellSize = new Vector3(CELL_SIZE, CELL_SIZE, 1f);
        _grid.cellGap = Vector3.zero;

        _playerPrefab = playerPrefab;
        _heavyEnemy = heavyEnemy;
        _lightEnemy = lightEnemy;
        _normalEnemy = normalEnemy;
        _cellMap = cellsMaps;

        _terminalConfiguration = terminalConfiguration;
        _defaultMat = defaultMat;

        _cellTypePerCoords = new Dictionary<Type, List<Vector2Int>>();
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
        EventBus.Subscribe<DevAddTerminalEvent>(OnDevAddTerminal);

        Build();
    }

    private GameObject CreateCellObject(Vector2Int coordinates)
    {
        GameObject goCell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goCell.name = $"Cell_{coordinates.x}_{coordinates.y}";
        goCell.layer = CELL_LAYER;
        goCell.GetComponent<MeshRenderer>().material = _defaultMat;

        goCell.transform.position = _grid.GetCellCenterWorld(new Vector3Int(coordinates.x, coordinates.y, 0));

        goCell.transform.localScale = new Vector3(_grid.cellSize.x, 1f, _grid.cellSize.y);

        goCell.transform.SetParent(_grid.transform, true);

        return goCell;
    }

    private void OnDevAddTerminal(in DevAddTerminalEvent addTerminalEvent)
    {
        if (addTerminalEvent.coordX < 0 || addTerminalEvent.coordX >= Width ||
            addTerminalEvent.coordY < 0 || addTerminalEvent.coordY >= Height)
            return;

        Cell cell = GetCell(addTerminalEvent.coordX, addTerminalEvent.coordY);

        if (cell.Terminal != null)
        {
            UnityEngine.Object.Destroy(cell.Terminal);
            cell.Terminal = null;
        }

        if (Enum.TryParse(addTerminalEvent.terminalTypeName, out TerminalType type))
        {
            Terminal terminal = cell.gameObject.AddComponent<Terminal>();
            terminal.SetType(type);
            terminal.Init(cell, _terminalConfiguration, _defaultMat);
            cell.Terminal = terminal;
        }
    }

    private void OnDevResizeGrid(in DevResizeGridEvent resizeGridEvent)
    {
        int newWidth = Mathf.Max(1, resizeGridEvent.width);
        int newHeight = Mathf.Max(1, resizeGridEvent.height);

        IEnumerator<Player> players = EntityRegistry.FilterEntities<Player>().GetEnumerator();
        players.MoveNext();
        Player player = players.Current;

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

        player.ClearPlan();

        Vector2Int playerCoord = player.CurrentCell.Coordinates;

        List<(Type enemyType, Vector2Int coord)> survivors = new();

        foreach (Enemy enemy in EntityRegistry.FilterEntities<Enemy>())
        {
            Vector2Int coord = enemy.CurrentCell.Coordinates;

            if (coord.x < newWidth && coord.y < newHeight)
                survivors.Add((enemy.GetType(), coord));

            EntityRegistry.Remove(enemy);
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
                Vector2Int coord = new Vector2Int(x, z);
                GameObject goCell = CreateCellObject(coord);

                Cell cellObject = goCell.AddComponent<Cell>();
                cellObject.SetCoordinate(coord);
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
            GameObject prefab = enemyType == typeof(HeavyEnemy) ? _heavyEnemy
                : enemyType == typeof(LightEnemy) ? _lightEnemy
                : enemyType == typeof(NormalEnemy) ? _normalEnemy
                : null;

            if (prefab == null)
                continue;

            GameObject go = UnityEngine.Object.Instantiate(prefab);
            Unit unit = (Unit)go.AddComponent(enemyType);
            unit.SetSpawnCell(GetCell(coord));
            unit.Init();
            EntityRegistry.Add(unit);
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
            nameof(HeavyEnemy) => _heavyEnemy,
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

    public void AddCell(Cell cell)
    {
        Type cellState = null;

        do
        {
            cellState = cellState == null ? cell.GetState() : cellState.BaseType;

            if (!_cellTypePerCoords.ContainsKey(cellState))
                _cellTypePerCoords.Add(cellState, new List<Vector2Int>());

            _cellTypePerCoords[cellState].Add(cell.Coordinates);

        } while (cellState != typeof(CellState));
    }

    public List<Cell> GetListOfState<CellType>() where CellType : CellState
    {
        Type cellState = typeof(CellType);

        if (!_cellTypePerCoords.ContainsKey(cellState))
            throw new KeyNotFoundException($"key {cellState.Name} was not found.");

        List<Cell> cells = new List<Cell>();

        foreach (Vector2Int coord in _cellTypePerCoords[cellState])
            cells.Add(_gridArray[coord.x, coord.y]);

        return cells;
    }

    public Cell GetRandom()
    {
        return GetRandomOfState<CellState>();
    }

    public Cell GetRandomOfState<CellType>() where CellType : CellState
    {
        Type cellState = typeof(CellType);

        if (!_cellTypePerCoords.ContainsKey(cellState))
            return null;

        List<Vector2Int> cellsOCoords = _cellTypePerCoords[cellState];

        int randomIndex = UnityEngine.Random.Range(0, cellsOCoords.Count);

        Vector2Int coordinate = cellsOCoords[randomIndex];

        return _gridArray[coordinate.x, coordinate.y];
    }

    public Cell GetRandomOfState<CellType>(Cell exeption) where CellType : CellState
    {
        Cell randomCell = null;

        do
        {
            randomCell = GetRandomOfState<CellType>();

        } while (exeption != null && exeption != randomCell);

        return randomCell;
    }

    public IEnumerator<Cell> GetAllOfState<CellType>() where CellType : CellState
    {
        Type cellState = typeof(CellType);

        if (!_cellTypePerCoords.ContainsKey(cellState))
            throw new KeyNotFoundException($"key {cellState.Name} was not found.");

        foreach (Vector2Int coord in _cellTypePerCoords[cellState])
            yield return _gridArray[coord.x, coord.y];
    }

    public void RemoveCell(Cell cell)
    {
        Type cellState = null;

        do
        {
            cellState = cellState == null ? cell.GetState() : cellState.BaseType;

            if (_cellTypePerCoords.ContainsKey(cellState))
                _cellTypePerCoords[cellState].Remove(cell.Coordinates);

        } while (cellState != typeof(CellState));
    }

    private void Build()
    {
        if (_defaultMat == null)
        {
            Debug.LogError("No default material provided");
            return;
        }

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
            GameObject goCell = CreateCellObject(cell._coordinates);

            Cell cellObject = goCell.AddComponent<Cell>();

            _gridArray[cell._coordinates.x, cell._coordinates.y] = cellObject;

            cellObject.SetCoordinate(cell._coordinates);
            cellObject.Init(_cellStatesPerName[cell._initialState], cell._cellVisualSet);

            switch (cell._spawnUnit)
            {
                case nameof(Player):
                    player.SetSpawnCell(cellObject);
                    break;

                case nameof(HeavyEnemy):
                    goEnemy = UnityEngine.Object.Instantiate(_heavyEnemy);
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
                    terminal.Init(cellObject, _terminalConfiguration, _defaultMat);
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
                AdjustEntityPositionToCell(decoration, goCell);
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

    public void AdjustEntityPositionToCell(GameObject entity, GameObject cellGO)
    {
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float cellSizeX = cellGO.transform.localScale.x;
            float cellSizeZ = cellGO.transform.localScale.z;

            float entitySizeX = bounds.size.x;
            float entitySizeZ = bounds.size.z;

            if (entitySizeX > 0f && entitySizeZ > 0f)
            {
                float scaleX = cellSizeX / entitySizeX;
                float scaleZ = cellSizeZ / entitySizeZ;
                float scale = Mathf.Min(scaleX, scaleZ);
                entity.transform.localScale *= scale;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds = renderers[i].bounds.min.y < bounds.min.y ? renderers[i].bounds : bounds;

            float cellTopY = cellGO.transform.position.y + (cellGO.transform.localScale.y * 0.5f);
            float offsetFromPivotToBottom = entity.transform.position.y - bounds.min.y;
            entity.transform.position = new Vector3(cellGO.transform.position.x, cellTopY + offsetFromPivotToBottom, cellGO.transform.position.z);
        }
        else
            entity.transform.position = Vector3.zero;
    }

    public void Tick(float deltaTime)
    {
        EventBus.Raise<InfectTilesEvent>();

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
        return _grid.GetCellCenterWorld(new Vector3Int(x, z, 0));
    }

    private void OnTileContagiousSpread(in InfectTilesEvent _)
    {
        if (IsBoardInfected())
            return;

        Cell selectedCell = GetRandomOfState<Contagious>(null);

        if (selectedCell == null)
            return;

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        ShuffleDirections(directions);

        Cell neighbor = null;

        foreach (Vector2Int direction in directions)
        {
            Vector2Int posToCheck = selectedCell.Coordinates + direction;

            bool outOfBounds = posToCheck.x >= Width || posToCheck.x < 0 ||
                                posToCheck.y >= Height || posToCheck.y < 0;

            if (outOfBounds)
                continue;

            Cell candidate = GetCell(posToCheck);
            if (candidate != null)
            {
                neighbor = candidate;
                break;
            }
        }

        selectedCell.Transition(typeof(Infected));

        if (neighbor != null &&
            neighbor.IsWalkable &&
            neighbor.GetState() != typeof(Infected) &&
            neighbor.GetState() != typeof(Contagious))
        {
            neighbor.Transition(typeof(Contagious));
        }

        bool IsBoardInfected()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cell cell = GetCell(new Vector2Int(x, y));

                    if (cell == null || !cell.IsWalkable)
                        continue;

                    Type state = cell.GetState();
                    if (state != typeof(Infected) && state != typeof(Contagious))
                        return false;
                }
            }

            return true;
        }

        void ShuffleDirections(Vector2Int[] directions)
        {
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (directions[i], directions[j]) = (directions[j], directions[i]);
            }
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