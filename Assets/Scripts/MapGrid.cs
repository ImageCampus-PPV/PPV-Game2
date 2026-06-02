using ImageCampus.ToolBox.Services;
using UnityEngine;

public class MapGrid : IService
{
    public bool IsPersistance => false;

    [SerializeField] private int _cellsX;
    [SerializeField] private int _cellsZ;
    [SerializeField] private float _cellsSize;
    public int Width => _cellsX;
    public int Height => _cellsZ;

    private Cell[,] _gridArray;
    [SerializeField] private GameObject _cellPrefab;

    public void Init()
    {
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        Cell[] cells = Object.FindObjectsOfType<Cell>();

        if (cells.Length == 0)
            return;

        (int minX, int minZ, int maxX, int maxZ) = GetMinMaxSize();

        _cellsX = maxX - minX + 1;
        _cellsZ = maxZ - minZ + 1;

        _gridArray = new Cell[_cellsX, _cellsZ];

        foreach (Cell cell in cells)
        {
            Vector2Int coord = cell.Coordinates;

            int x = coord.x - minX;
            int z = coord.y - minZ;

            _gridArray[x, z] = cell;
        }

        (int minX, int minZ, int maxX, int maxZ) GetMinMaxSize()
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;

            foreach (Cell cell in cells)
            {
                Vector2Int coord = cell.Coordinates;

                if (coord.x < minX) minX = coord.x;
                if (coord.x > maxX) maxX = coord.x;

                if (coord.y < minZ) minZ = coord.y;
                if (coord.y > maxZ) maxZ = coord.y;
            }

            return (minX, minZ, maxX, maxZ);
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
        return _gridArray[x, z].transform.position;
        //return
        //    new Vector3(x, 0, 0) * _cellsSize +
        //    new Vector3(0, 0, z) * _cellsSize * VERTICAL_OFFSET_MULTIPLIER +
        //    ((Mathf.Abs(z) % 2) == 1 ? new Vector3(1, 0, 0) * _cellsSize * .5f : Vector3.zero);
    }

}
