using ImageCampus.ToolBox.Services;
using UnityEngine;

public class TileHoverHighlighter : IService
{
    public bool IsPersistance => false;

    [SerializeField] private Color _hoverColor = Color.cyan;
    [SerializeField] private LayerMask _cellLayerMask = 1 << 6;

    private Camera _camera;
    private Cell _hoveredCell;
    private Color _originalColor;

    public TileHoverHighlighter(Camera camera = null, Color? hoverColor = null, LayerMask? cellLayerMask = null)
    {
        _camera = camera != null ? camera : Camera.main;

        if (hoverColor.HasValue)
            _hoverColor = hoverColor.Value;

        if (cellLayerMask.HasValue)
            _cellLayerMask = cellLayerMask.Value;
    }

    public void Tick()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera == null)
            return;

        Cell newHoveredCell = GetCellUnderCursor();

        if (newHoveredCell == _hoveredCell)
            return;

        RestoreHoveredCellColor();

        if (newHoveredCell != null && newHoveredCell.Renderer != null)
        {
            _hoveredCell = newHoveredCell;
            _originalColor = _hoveredCell.Renderer.material.color;
            _hoveredCell.Renderer.material.color = _hoverColor;
        }
    }

    private Cell GetCellUnderCursor()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _cellLayerMask))
            return hit.collider.GetComponent<Cell>();

        return null;
    }

    private void RestoreHoveredCellColor()
    {
        if (_hoveredCell == null)
            return;

        if (_hoveredCell.Renderer != null)
            _hoveredCell.Renderer.material.color = _originalColor;

        _hoveredCell = null;
    }
}