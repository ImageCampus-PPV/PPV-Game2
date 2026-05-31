using UnityEditor;
using UnityEngine;

public class Cell : MonoBehaviour
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
    [HideInInspector] public bool isOccupied;

    private Bounds _bounds;

    public bool IsWalkable => _isWalkable;
    public bool IsHeightConnector => _isHeightConnector;
    public bool ProvidesCover => _providesCover;
    public int Height => _height;
    public float WorldHeight => _height * _heightStep;
    public Vector2Int Coordinates => _coordinates;

    public int CalculateFCost()
    {
        fCost = gCost + hCost;
        return fCost;
    }

    private void OnValidate()
    {
        ApplyHeight();
    }

    private void Awake()
    {
        _bounds = GetComponentInChildren<Renderer>().bounds;
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