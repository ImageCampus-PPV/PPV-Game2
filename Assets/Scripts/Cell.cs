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

    public bool IsWalkable => _isWalkable;
    public bool IsHeightConnector => _isHeightConnector;
    public bool ProvidesCover => _providesCover;
    public int Height => _height;
    public float WorldHeight => _height * _heightStep;
    public Vector2Int Coordinates => _coordinates;


    public void SetCoordinates(Vector2Int coordinates)
    {
        _coordinates = coordinates;
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
        Renderer rend = GetComponentInChildren<Renderer>();

        return new Vector3(transform.position.x, rend.bounds.max.y, transform.position.z
        );
    }
}