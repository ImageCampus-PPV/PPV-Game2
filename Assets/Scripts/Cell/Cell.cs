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

    [Header("Pathfinding")]
    [HideInInspector] public int gCost;
    [HideInInspector] public int hCost;
    [HideInInspector] public int fCost;
    [HideInInspector] public Cell cameFromCell;
    [HideInInspector] public bool isOccupied => stander != null;
    public Unit stander = null;

    // Terminal hackeable ubicada sobre esta Cell (MEC-02), si es que tiene una. Asignada por MapGrid.Build().
    public Terminal Terminal { get; set; }

    private Bounds _bounds;

    private FSM _fsm;

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

    public void Init(Type initialState)
    {
        _fsm = new FSM(typeof(DefaultCell));

        _bounds = GetComponent<MeshRenderer>().bounds;

        Renderer renderer = GetComponent<Renderer>();

        _fsm.AddState<DefaultCell>(onEnterParameters: () => new object[] { renderer });
        _fsm.AddState<Unstable>(onEnterParameters: () => new object[] { renderer, 1 });
        _fsm.AddState<Broken>(onEnterParameters: () => new object[] { renderer });
        _fsm.AddState<Healing>(onEnterParameters: () => new object[] { renderer }, onTickParameters: () => new object[] { stander });
        _fsm.AddState<Contagious>(onEnterParameters: () => new object[] { 10u, _coordinates, renderer }, onTickParameters: () => new object[] { stander });
        _fsm.AddState<Infected>(onEnterParameters: () => new object[] { 10u, renderer }, onTickParameters: () => new object[] { stander });

        _fsm.Transition(initialState);
    }

    public void Tick(float deltTime)
    {
        if (!_justTransitioned)
            _fsm.Tick();

        _justTransitioned = false;
    }

    public void Transition(Type type)
    {
        _justTransitioned = true;
        _fsm.Transition(type);
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
        return new Vector3(transform.position.x, _bounds.max.y, transform.position.z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.Label(transform.position + Vector3.up, $"X: {Coordinates.x}. Z: {Coordinates.y}.");
    }
#endif
}