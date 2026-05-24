using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MyGrid : MonoBehaviour
{
    [SerializeField] private int _cellsX;
    [SerializeField] private int _cellsZ;
    [SerializeField] private float _cellsSize;

    public int Width => _cellsX;
    public int Height => _cellsZ;

    private const float VERTICAL_OFFSET_MULTIPLIER = 0.866f;
    private const float HORIZONTAL_OFFSET_MULTIPLIER = 0.5f;

    private Cell[,] _gridArray;
    [SerializeField] private GameObject _cellPrefab;

    //private static Grid _instance;
    //public static Grid Instance
    //{
    //    get
    //    {
    //        if (_instance == null)
    //            _instance = new Grid();
    //
    //        return _instance;
    //    }
    //    private set
    //    {
    //        _instance = value;
    //    }
    //}


    private void Awake()
    {
        if (_gridArray == null || _gridArray.Length == 0)
        {
            RebuildGrid();
        }
    }


    [ContextMenu("Crate Grid")]
    private void CreateGrid()
    {
        DeleteGrid();
        _gridArray = new Cell[_cellsX, _cellsZ];

        Renderer prefabRenderer = _cellPrefab.GetComponentInChildren<Renderer>();
        float originalWidth = prefabRenderer.bounds.size.x;
        float scaleFactor = _cellsSize / originalWidth;

        for (int x = 0; x < _cellsX; x++)
        {
            for (int z = 0; z < _cellsZ; z++)
            {
                GameObject newCell = Instantiate(_cellPrefab, transform);
                newCell.transform.localScale = Vector3.one * scaleFactor;

                // Vector3 position = new Vector3(x, 0, 0) * _cellsSize +
                //     new Vector3(0, 0, z) * _cellsSize * VERTICAL_OFFSET_MULTIPLIER +
                //     ((z % 2 == 1) ? Vector3.right * _cellsSize * HORIZONTAL_OFFSET_MULTIPLIER : Vector3.zero);
                // newCell.transform.localPosition = position;

                Vector3 position = new Vector3(x, 0, 0) * newCell.transform.localScale.x + new Vector3(0, 0, z) * newCell.transform.localScale.z;

                newCell.transform.localPosition = position;
                newCell.transform.localScale *= 0.9f;

                Cell cell = newCell.GetComponent<Cell>();
                if (cell == null)
                    cell = newCell.AddComponent<Cell>();

                cell.SetCoordinates(new Vector2Int(x, z));

                newCell.name = $"Cell: {x}, {z}";

                _gridArray[x, z] = cell;
            }
        }
    }

    [ContextMenu("Rebuild Grid From Scene")]
private void RebuildGrid()
{
    _gridArray = new Cell[_cellsX, _cellsZ];

    foreach (Transform child in transform)
    {
        Cell cell = child.GetComponent<Cell>();
        if (cell == null) continue;

        Vector2Int coord = cell.Coordinates;
        _gridArray[coord.x, coord.y] = cell;
    }
}

    [ContextMenu("Delete Grid")]
    private void DeleteGrid()
    {
        List<GameObject> children = new List<GameObject>();

        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        for (int i = children.Count - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(children[i]);
#else
            Destroy(children[i]);
#endif
        }
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
        return
            new Vector3(x, 0, 0) * _cellsSize +
            new Vector3(0, 0, z) * _cellsSize * VERTICAL_OFFSET_MULTIPLIER +
            ((Mathf.Abs(z) % 2) == 1 ? new Vector3(1, 0, 0) * _cellsSize * .5f : Vector3.zero);
    }

}
