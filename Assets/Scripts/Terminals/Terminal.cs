using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;
using UnityEngine.Events;

public class Terminal : BaseEntity
{
    [Header("Config")]
    [SerializeField] private TerminalType _type;
    [SerializeField] private TerminalState _initialState = TerminalState.Active;

    [Header("Balance (0 = usar TerminalConfiguration)")]
    [Tooltip("Si es 0, se toma el valor por defecto del tipo de terminal desde TerminalConfiguration.")]
    [SerializeField] private int _apCostOverride = 0;
    [SerializeField] private int _requiredTicksOverride = 0;
    [SerializeField] private int _rangeOverride = 0;

    [Header("Feedback / Efecto")]
    [Tooltip("Se invoca cuando el hackeo se completa exitosamente. Usar para abrir puertas, dar buffs, avanzar objetivos, etc.")]
    [SerializeField] private UnityEvent _onHackCompleted;

    private TerminalState _state;
    private int _currentTicks;
    private Cell _cell;

    private int _apCost;
    private int _requiredTicks;
    private int _range;

    private bool _initialized;

    private GameObject _marker;
    private Renderer _markerRenderer;
    private Material _defaultMat;

    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    public TerminalType Type => _type;
    public Cell Cell => _cell;
    public int CurrentTicks => _currentTicks;
    public int APCost => _apCost;
    public int RequiredTicks => _requiredTicks;
    public int Range => _range;

    public TerminalState RawState => _state;


    public TerminalState EffectiveState
    {
        get
        {
            if (_state == TerminalState.InProgress || _state == TerminalState.Completed)
                return _state;

            if (IsCellCorrupted())
                return TerminalState.Corrupted;

            return _state;
        }
    }

    public bool IsComplete => _state == TerminalState.Completed;

    public void SetType(TerminalType type)
    {
        _type = type;
    }

    public void Init(Cell cell, TerminalConfiguration configuration, Material defaultMat)
    {
        _cell = cell;

        TerminalBalanceData balance = configuration != null
            ? configuration.GetBalance(_type)
            : new TerminalBalanceData { type = _type, apCost = 1, requiredTicks = 1, range = 1 };

        _apCost = _apCostOverride > 0 ? _apCostOverride : balance.apCost;
        _requiredTicks = _requiredTicksOverride > 0 ? _requiredTicksOverride : balance.requiredTicks;
        _range = _rangeOverride > 0 ? _rangeOverride : balance.range;

        _currentTicks = 0;
        _initialized = true;

        if (defaultMat == null)
            Debug.LogError("No default material provided to Terminal");

        _defaultMat = defaultMat;

        CreateMarker();
        SetState(_initialState);
    }

    public void SetState(TerminalState newState)
    {
        _state = newState;

        UpdateMarkerVisual();

        if (_initialized)
            EventBus.Raise<TerminalStateChangedEvent>(ID, EffectiveState);
    }

    private void CreateMarker()
    {
        _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _marker.name = $"TerminalMarker ({_type})";

        Collider markerCollider = _marker.GetComponent<Collider>();
        if (markerCollider != null)
            Destroy(markerCollider);

        _marker.transform.SetParent(transform, worldPositionStays: false);
        _marker.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        _marker.transform.localScale = Vector3.one * 0.35f;

        _markerRenderer = _marker.GetComponent<Renderer>();

        _markerRenderer.material = _defaultMat;
    }

    private void UpdateMarkerVisual()
    {
        if (_markerRenderer == null)
            return;

        Color color;

        switch (EffectiveState)
        {
            case TerminalState.Active:
                color = Color.cyan;
                break;
            case TerminalState.InProgress:
                color = Color.yellow;
                break;
            case TerminalState.Corrupted:
                color = new Color(1f, 0.15f, 0.6f);
                break;
            case TerminalState.Completed:
                color = Color.green;
                break;
            case TerminalState.Blocked:
            case TerminalState.Inactive:
                color = Color.gray;
                break;
            default:
                color = Color.white;
                break;
        }

        _markerRenderer.material.color = color;
    }

    public bool CanBeHacked()
    {
        TerminalState effective = EffectiveState;

        return effective == TerminalState.Active
            || effective == TerminalState.InProgress
            || effective == TerminalState.Corrupted;
    }

    public bool AdvanceProgress()
    {
        if (_currentTicks == 0)
            SetState(TerminalState.InProgress);

        _currentTicks++;

        EventBus.Raise<HackProgressEvent>(ID, _currentTicks, _requiredTicks);

        if (_currentTicks < _requiredTicks)
            return false;

        SetState(TerminalState.Completed);
        EventBus.Raise<HackCompletedEvent>(ID, _type);
        ApplyBuiltInEffect();
        _onHackCompleted?.Invoke();

        return true;
    }

    public void Interrupt()
    {
        if (_state != TerminalState.InProgress)
            return;

        EventBus.Raise<HackInterruptedEvent>(ID, _currentTicks);
    }

    private bool IsCellCorrupted()
    {
        if (_cell == null)
            return false;

        System.Type cellState = _cell.GetState();

        return cellState == typeof(Infected) || cellState == typeof(Contagious);
    }

    private void ApplyBuiltInEffect()
    {
        switch (_type)
        {
            case TerminalType.Purification:
                PurifyNearbyCells();
                break;

            case TerminalType.Combat:
                StunNearbyEnemies();
                break;
        }
    }

    private void PurifyNearbyCells()
    {
        if (_cell == null)
            return;

        MapGrid mapGrid = ServiceProvider.Instance.GetService<MapGrid>();

        TryPurify(_cell);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int origin = _cell.Coordinates;

        foreach (Vector2Int dir in directions)
        {
            Vector2Int coord = origin + dir;

            if (coord.x < 0 || coord.y < 0 || coord.x >= mapGrid.Width || coord.y >= mapGrid.Height)
                continue;

            TryPurify(mapGrid.GetCell(coord));
        }

        void TryPurify(Cell cell)
        {
            if (cell == null)
                return;

            System.Type state = cell.GetState();

            if (state == typeof(Infected) || state == typeof(Contagious))
                cell.Transition(typeof(DefaultCell));
        }
    }

    private void StunNearbyEnemies()
    {
        if (_cell == null)
            return;

        TurnManager turnManager = ServiceProvider.Instance.GetService<TurnManager>();
        EntityRegistry entityRegistry = ServiceProvider.Instance.GetService<EntityRegistry>();

        int stunnedCount = 0;

        foreach (Enemy enemy in entityRegistry.FilterEntities<Enemy>())
        {
            if (enemy.CurrentCell == null)
                continue;

            if (turnManager.IsCellNearUnit(_cell, enemy.CurrentCell, _range))
            {
                turnManager.ApplyStun(enemy);
                stunnedCount++;
            }
        }

        Debug.Log(stunnedCount > 0
            ? $"[Terminal] Combate en {_cell.Coordinates}: {stunnedCount} enemigo(s) stuneado(s)."
            : $"[Terminal] Combate en {_cell.Coordinates}: no habia ningun enemigo en linea recta dentro de rango {_range} en el momento de completarse el hackeo.");
    }
}
