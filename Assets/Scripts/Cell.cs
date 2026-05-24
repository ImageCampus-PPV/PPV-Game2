using UnityEngine;

public class Cell : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private bool _isWalkable = true;
    [SerializeField] private bool _isHeightConnector;
    [SerializeField] private int _height;

    [Header("Visual")]
    [SerializeField] private float _heightStep = 1f;

    [SerializeField] private Vector2Int _coordinates;

    public int gCost;
    public int hCost;
    public int fCost;
    public Cell cameFromCell;

    public bool IsWalkable => _isWalkable;
    public bool IsHeightConnector => _isHeightConnector;
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
        float topY = rend.bounds.max.y;

        return new Vector3(
            transform.position.x,
            topY + transform.localScale.y * 0.5f,
            transform.position.z
        );
    }
}