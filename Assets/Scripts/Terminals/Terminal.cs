using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;
using UnityEngine.Events;

// MEC-02 - GDD de Hackeo de Terminales
// Representa una terminal hackeable ubicada sobre una Cell de la grilla.
// El "Hackeo" en si (declarar la accion, gastar AP/ticks, resolverla) lo maneja
// HackSystem; esta clase modela el objeto/estado de la terminal (ver
// "Diferencia entre Terminal y Hackeo" en el GDD).
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

    // Marcador visual simple (placeholder de programador) para poder
    // distinguir a simple vista una celda con Terminal del resto: una esferita
    // flotando arriba del tile, coloreada segun EffectiveState. No toca el
    // Renderer de la Cell (ese lo maneja la FSM de Cell/DefaultCell.cs) para
    // no pisarle el color a la corrupcion/estados de la celda.
    private GameObject _marker;
    private Renderer _markerRenderer;

    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    public TerminalType Type => _type;
    public Cell Cell => _cell;
    public int CurrentTicks => _currentTicks;
    public int APCost => _apCost;
    public int RequiredTicks => _requiredTicks;
    public int Range => _range;

    // Estado "crudo" (el que se setea explicitamente).
    public TerminalState RawState => _state;

    // Estado que efectivamente deberia mostrar/usar la UI: si la Cell esta
    // corrupta (Infected/Contagious, ver Cell/DefaultCell.cs) y la terminal no
    // esta en medio de un hackeo ni completada, se reporta como Corrupted.
    // Esto evita duplicar el estado de corrupcion en dos lugares distintos.
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

    public void Init(Cell cell, TerminalConfiguration configuration)
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
            Destroy(markerCollider); // no debe interceptar los raycasts de movimiento/abilities/hack

        _marker.transform.SetParent(transform, worldPositionStays: false);
        _marker.transform.localPosition = new Vector3(0f, 0.9f, 0f); // flota arriba del cubo de la celda
        _marker.transform.localScale = Vector3.one * 0.35f;

        _markerRenderer = _marker.GetComponent<Renderer>();
    }

    // NOTA: esto solo se re-evalua cuando se llama a SetState() (Init, avance
    // de hackeo, etc). Si la Cell se corrompe despues por su cuenta (spread de
    // Contagious) sin que la Terminal reciba un SetState, el marcador puede
    // quedar visualmente desactualizado un instante aunque CanBeHacked()/
    // EffectiveState ya reflejen la corrupcion correctamente para el gameplay.
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

    // Condiciones para iniciar un Hackeo (ver GDD): la terminal debe estar
    // activa, en proceso (retomando un hackeo interrumpido) o corrupta
    // (hackeable igual, con riesgo extra). Inactiva y Bloqueada no se pueden
    // hackear.
    public bool CanBeHacked()
    {
        TerminalState effective = EffectiveState;

        return effective == TerminalState.Active
            || effective == TerminalState.InProgress
            || effective == TerminalState.Corrupted;
    }

    // Avanza un tick de progreso. Devuelve true si con este tick se completo el hackeo.
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

    // El progreso NO se revierte al interrumpir (ver "Hackeos Interrumpidos" /
    // "Break DESPUES de que empiece el hackeo" en el GDD): la terminal queda
    // "En Proceso" y se retoma en una proxima fase de planeamiento.
    // TODO(MEC-06 - Break): cuando se implemente Break, este es el punto de
    // enganche para interrumpir un hackeo en curso.
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

    // Efectos "built-in" para los tipos de terminal que ya tienen un sistema
    // real para apoyarse (corrupcion de Cell y stun de TurnManager). El resto
    // de los tipos (Access, Reward, FloorObjective, Influence "global") no
    // tienen todavia un sistema propio (puertas, buffs, objetivos de piso,
    // Influencia de la IDOL - SIS-05) asi que se resuelven unicamente via el
    // UnityEvent _onHackCompleted, para que se puedan enganchar desde el nivel
    // sin inventar sistemas que no existen aun.
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

            // OJO: IsCellNearUnit solo busca en linea recta (N/S/E/O) desde la
            // celda de la Terminal, no en diagonal ni por distancia Manhattan
            // general (mismo chequeo que usan Counter/LagSpike). Un enemigo
            // que se movio fuera de esa linea, aunque este "cerca" en
            // distancia real, no cuenta como en rango.
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
