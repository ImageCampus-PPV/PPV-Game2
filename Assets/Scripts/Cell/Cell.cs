using System;
using UnityEditor;
using UnityEngine;

public class Cell : BaseEntity
{
    [Header("Config")]
    [SerializeField] private bool _isWalkable = true;
    [SerializeField] private bool _isHeightConnector;
    [SerializeField] private bool _providesCover;
    [SerializeField] private int _height;

    [Header("Visual")]
    [SerializeField] private float _heightStep = 1f;
    [SerializeField] private Vector2Int _coordinates;

    private CellVisualSet _cellVisualSet;
    private GameObject _visualInstance;
    private GameObject _currentVisualPrefab;
    private Type _visualStateType;
    private Renderer _logicRenderer;

    [Header("Pathfinding")]
    [HideInInspector] public int gCost;
    [HideInInspector] public int hCost;
    [HideInInspector] public int fCost;
    [HideInInspector] public Cell cameFromCell;
    [HideInInspector] public bool isOccupied => stander != null || Terminal != null;
    public Unit stander = null;

    public Terminal Terminal { get; set; }

    private Bounds _bounds;

    private FSM _fsm;

    private Renderer _renderer;
    public Renderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<Renderer>());

    public bool IsWalkable => _isWalkable && _fsm.GetState() != typeof(Broken);
    public bool IsHeightConnector => _isHeightConnector;
    public bool ProvidesCover => _providesCover;
    public int Height => _height;
    public float WorldHeight => _height * _heightStep;
    public Vector2Int Coordinates => _coordinates;

    private bool _justTransitioned = false;

    public void SetCoordinate(Vector2Int coordinates)
    {
        _coordinates = coordinates;
    }

    public void Init(Type initialState, CellVisualSet visualSet = null)
    {
        _cellVisualSet = visualSet;
        _logicRenderer = GetComponent<Renderer>();

        _fsm = new FSM(typeof(DefaultCell));

        Renderer renderer = _logicRenderer;

        _fsm.AddState<DefaultCell>(
            onEnterParameters: () => new object[] { this },
            onExitParameters: () => new object[] { this });

        _fsm.AddState<Empty>(
            onEnterParameters: () => new object[] { this, renderer },
            onExitParameters: () => new object[] { this });

        _fsm.AddState<Unstable>(
            onEnterParameters: () => new object[] { this, renderer, 1 },
            onExitParameters: () => new object[] { this });

        _fsm.AddState<Broken>(
            onEnterParameters: () => new object[] { this, renderer },
            onExitParameters: () => new object[] { this });

        _fsm.AddState<Healing>(
            onEnterParameters: () => new object[] { this, renderer },
            onTickParameters: () => new object[] { stander },
            onExitParameters: () => new object[] { this });

        _fsm.AddState<Contagious>(
            onEnterParameters: () => new object[] { this, 10u, _coordinates, renderer },
            onTickParameters: () => new object[] { stander },
            onExitParameters: () => new object[] { this });

        _fsm.AddState<Infected>(
            onEnterParameters: () => new object[] { this, 10u, renderer },
            onTickParameters: () => new object[] { stander },
            onExitParameters: () => new object[] { this });

        _fsm.Transition(initialState);

        UpdateVisual(_fsm.GetState());
    }

    private void UpdateVisual(Type stateType)
    {
        if (stateType == null)
            return;

        _visualStateType = stateType;

        if (_cellVisualSet == null)
            return;

        GameObject prefab = _cellVisualSet.GetPrefab(stateType);

        if (prefab != null && prefab == _currentVisualPrefab && _visualInstance != null)
            return;

        if (_visualInstance != null)
            Destroy(_visualInstance);

        _visualInstance = null;
        _currentVisualPrefab = null;

        if (prefab == null)
        {
            if (_logicRenderer != null)
                _logicRenderer.enabled = true;

            return;
        }

        _visualInstance = Instantiate(prefab, transform, false);
        _visualInstance.transform.localPosition = Vector3.zero;
        _visualInstance.name = $"{prefab.name}_Visual";

        _currentVisualPrefab = prefab;

        if (_logicRenderer != null)
            _logicRenderer.enabled = false;
    }

    public void Transition(Type type)
    {
        _justTransitioned = true;
        _fsm.Transition(type);

        UpdateVisual(_fsm.GetState());
    }

    public void Tick(float deltTime)
    {
        if (!_justTransitioned)
            _fsm.Tick();

        Type currentState = _fsm.GetState();

        if (currentState != _visualStateType)
            UpdateVisual(currentState);

        _justTransitioned = false;
    }

    public Type GetState()
    {
        return _fsm.GetState();
    }

    public int CalculateFCost()
    {
        fCost = gCost + hCost;
        return fCost;
    }

    private void OnValidate()
    {
        ApplyHeight();
    }


    private void Start()
    {
        ApplyHeight();
    }

    private void ApplyHeight()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, WorldHeight, transform.localPosition.z);
    }

    public Vector3 GetWorldTopPosition()
    {
        Collider cellCollider = GetComponent<Collider>();
        float topY = cellCollider != null ? cellCollider.bounds.max.y : transform.position.y;

        return new Vector3(transform.position.x, topY, transform.position.z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.Label(transform.position + Vector3.up, $"X: {Coordinates.x}. Z: {Coordinates.y}.");
    }
#endif
}