#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class FloorEditor : EditorWindow
{
    private int _rows = 10;
    private int _cols = 10;

    private float _cellSize = 60f;

    private VisualElement _gridContainer;
    private ScrollView _scrollView;

    private IntegerField _rowsField;
    private IntegerField _colsField;

    private CellData[,] _cells;

    private Dictionary<Vector2Int, Button> _cellButtons;

    private string _cellsMapSavePath = "Assets/Maps/CellsMap.asset";
    private Floor _cellsMapAsset;

    private Dictionary<string, Color> _cellsColorPerState;

    [MenuItem("Tools/Floor Editor")]
    public static void ShowWindow()
    {
        GetWindow<FloorEditor>("Floor Editor");
    }

    public void CreateGUI()
    {
        _cellButtons = new Dictionary<Vector2Int, Button>();
        _cellsColorPerState = new Dictionary<string, Color>();

        foreach (Type type in GetType().Assembly.GetTypes())
        {
            CellStateAttribute cellAtribute = type.GetCustomAttribute<CellStateAttribute>();

            if (cellAtribute == null)
                continue;

            _cellsColorPerState.Add(type.Name, new Color(cellAtribute.r, cellAtribute.g, cellAtribute.b, cellAtribute.a));
        }

        VisualElement root = rootVisualElement;

        _rowsField = new IntegerField("Rows") { value = _rows };
        _colsField = new IntegerField("Columns") { value = _cols };

        Slider zoomSlider = new Slider("Zoom", 20, 120);
        zoomSlider.value = _cellSize;

        Button rebuildButton = new Button(RebuildGrid)
        {
            text = "Apply Size"
        };

        ObjectField cellsMapObjectField = new ObjectField("Cells Map")
        {
            objectType = typeof(Floor),
            allowSceneObjects = false
        };
        cellsMapObjectField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<Floor>(_cellsMapSavePath));

        TextField cellsMapPathField = new TextField("Cells Map Path") { value = _cellsMapSavePath };
        Button saveCellsMapButton = new Button(SaveGridAsCellsMap) { text = "Save Grid As Cells Map" };
        Button loadCellsMapButton = new Button(LoadGridFromCellsMap) { text = "Load Grid From Cells Map" };
        Button selectCellsMapButton = new Button(SelectCellsMapAsset) { text = "Select Asset" };
        Button clearMapButton = new Button(ClearFloor) { text = "Clear Floor" };

        root.Add(_rowsField);
        root.Add(_colsField);
        root.Add(zoomSlider);
        root.Add(rebuildButton);
        root.Add(cellsMapObjectField);
        root.Add(cellsMapPathField);
        root.Add(saveCellsMapButton);
        root.Add(loadCellsMapButton);
        root.Add(selectCellsMapButton);
        root.Add(clearMapButton);

        _rowsField.RegisterValueChangedCallback(evt => _rows = Mathf.Max(1, evt.newValue));
        _colsField.RegisterValueChangedCallback(evt => _cols = Mathf.Max(1, evt.newValue));

        cellsMapObjectField.RegisterValueChangedCallback(evt =>
        {
            Floor droppedAsset = evt.newValue as Floor;
            if (droppedAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(droppedAsset);
            bool isCellsMapAsset = !string.IsNullOrEmpty(path) && path.EndsWith(".asset");

            if (!isCellsMapAsset)
            {
                Debug.LogWarning("Drop a CellsMaps asset from the Project window, not a scene object.");
                cellsMapObjectField.SetValueWithoutNotify(null);
                return;
            }

            _cellsMapSavePath = path;
            _cellsMapAsset = droppedAsset;
            cellsMapPathField.SetValueWithoutNotify(_cellsMapSavePath);
        });

        cellsMapPathField.RegisterValueChangedCallback(evt =>
        {
            _cellsMapSavePath = evt.newValue;
            _cellsMapAsset = AssetDatabase.LoadAssetAtPath<Floor>(_cellsMapSavePath);
            cellsMapObjectField.SetValueWithoutNotify(_cellsMapAsset);
        });

        zoomSlider.RegisterValueChangedCallback(evt =>
        {
            _cellSize = evt.newValue;
            RebuildGrid();
        });

        _scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        _scrollView.style.flexGrow = 1;
        root.Add(_scrollView);

        _gridContainer = new VisualElement();
        _scrollView.Add(_gridContainer);

        RebuildGrid();
    }

    private void ClearFloor()
    {
        for (int i = 0; i < _rows; ++i)
            for (int j = 0; j < _cols; ++j)
            {
                _cells[i, j]._spawnUnit = "None";
                _cells[i, j]._initialState = nameof(DefaultCell);
                _cells[i, j]._terminalType = "None";

                ApplyCellVisual(_cellButtons[_cells[i, j]._coordinates], _cells[i, j]);
            }
    }

    private bool TryValidateCellsMapPath()
    {
        if (string.IsNullOrEmpty(_cellsMapSavePath) || !_cellsMapSavePath.EndsWith(".asset"))
        {
            Debug.LogWarning("Cells map path must end with .asset, e.g. Assets/Maps/CellsMap.asset");
            return false;
        }

        return true;
    }

    private CellData[] FlattenCells()
    {
        CellData[] flat = new CellData[_rows * _cols];
        int index = 0;

        for (int i = 0; i < _rows; ++i)
        {
            for (int j = 0; j < _cols; ++j)
            {
                flat[index++] = _cells[i, j];
            }
        }

        return flat;
    }

    private bool TryBuildCellsFromFlatArray(CellData[] flatCells)
    {
        if (flatCells == null || flatCells.Length == 0)
            return false;

        int maxRow = -1;
        int maxCol = -1;

        foreach (CellData c in flatCells)
        {
            maxRow = Mathf.Max(maxRow, c._coordinates.x);
            maxCol = Mathf.Max(maxCol, c._coordinates.y);
        }

        if (maxRow < 0 || maxCol < 0)
            return false;

        int newRows = maxRow + 1;
        int newCols = maxCol + 1;

        CellData[,] loadedCells = new CellData[newRows, newCols];
        foreach (CellData c in flatCells)
        {
            loadedCells[c._coordinates.x, c._coordinates.y] = c;
        }

        _cells = loadedCells;
        _rows = newRows;
        _cols = newCols;
        return true;
    }

    private void SaveGridAsCellsMap()
    {
        if (!TryValidateCellsMapPath())
            return;

        string directory = System.IO.Path.GetDirectoryName(_cellsMapSavePath);
        if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        Floor asset = AssetDatabase.LoadAssetAtPath<Floor>(_cellsMapSavePath);
        bool isNewAsset = asset == null;

        if (isNewAsset)
        {
            asset = ScriptableObject.CreateInstance<Floor>();
        }

        asset._cellsData = FlattenCells();

        if (isNewAsset)
        {
            AssetDatabase.CreateAsset(asset, _cellsMapSavePath);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _cellsMapAsset = asset;

        Debug.Log($"Saved grid to {_cellsMapSavePath}");
    }

    private void LoadGridFromCellsMap()
    {
        if (!TryValidateCellsMapPath())
            return;

        Floor asset = AssetDatabase.LoadAssetAtPath<Floor>(_cellsMapSavePath);
        if (asset == null)
        {
            Debug.LogWarning($"No CellsMaps asset found at {_cellsMapSavePath}");
            return;
        }

        if (!TryBuildCellsFromFlatArray(asset._cellsData))
        {
            Debug.LogWarning("CellsMaps asset has no valid cell data; grid is unchanged.");
            return;
        }

        _cellsMapAsset = asset;
        _cellButtons.Clear();

        _rowsField?.SetValueWithoutNotify(_rows);
        _colsField?.SetValueWithoutNotify(_cols);

        RebuildGrid();

        Debug.Log($"Loaded grid from {_cellsMapSavePath}");
    }

    private void SelectCellsMapAsset()
    {
        if (!TryValidateCellsMapPath())
            return;

        Floor asset = AssetDatabase.LoadAssetAtPath<Floor>(_cellsMapSavePath);
        if (asset == null)
        {
            Debug.LogWarning($"No CellsMaps asset found at {_cellsMapSavePath}");
            return;
        }

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    private void RebuildGrid()
    {
        _gridContainer.Clear();

        if (_cellsMapAsset == null)
            _cellsMapAsset = new Floor();

        _cellsMapAsset.size = new Vector2Int(_rows, _cols);

        if (_cells == null && _cellsMapAsset != null)
        {
            TryBuildCellsFromFlatArray(_cellsMapAsset._cellsData);
        }

        bool needsResize = _cells == null ||
                           _cells.GetLength(0) != _rows ||
                           _cells.GetLength(1) != _cols;

        if (needsResize)
        {
            CellData[,] oldCells = _cells;
            int oldRows = oldCells?.GetLength(0) ?? 0;
            int oldCols = oldCells?.GetLength(1) ?? 0;

            for (int i = 0; i < oldRows; ++i)
            {
                for (int j = 0; j < oldCols; ++j)
                {
                    bool outOfBounds = i >= _rows || j >= _cols;
                    if (outOfBounds)
                    {
                        _cellButtons.Remove(new Vector2Int(i, j));
                    }
                }
            }

            CellData[,] newCells = new CellData[_rows, _cols];

            for (int i = 0; i < _rows; ++i)
            {
                for (int j = 0; j < _cols; ++j)
                {
                    bool reusable = i < oldRows && j < oldCols;

                    newCells[i, j] = reusable
                        ? oldCells[i, j]
                        : new CellData(new Vector2Int(i, j), nameof(DefaultCell));
                }
            }

            _cells = newCells;
        }

        _gridContainer.style.flexDirection = FlexDirection.Column;

        for (int i = 0; i < _rows; i++)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            for (int j = 0; j < _cols; j++)
            {
                int cx = i;
                int cy = j;

                Button cellButton = new Button(() => OnCellClicked(cx, cy));
                cellButton.text = $"{cx},{cy}";
                cellButton.style.width = _cellSize;
                cellButton.style.height = _cellSize;

                ApplyCellVisual(cellButton, _cells[i, j]);

                _cellButtons[new Vector2Int(i, j)] = cellButton;

                row.Add(cellButton);
            }

            _gridContainer.Add(row);
        }
    }

    private void OnCellClicked(int x, int y)
    {
        CellEditorWindow.ShowWindow(_cells[x, y], updatedCell => OnCellStateChanged(x, y, updatedCell));
    }

    private void OnCellStateChanged(int x, int y, CellData updatedCell)
    {
        _cells[x, y] = updatedCell;

        if (_cellButtons.TryGetValue(new Vector2Int(x, y), out Button btn))
            ApplyCellVisual(btn, updatedCell);
    }

    private void ApplyCellVisual(Button btn, CellData cell)
    {
        if (!_cellsColorPerState.TryGetValue(cell._initialState, out Color bg))
        {
            Debug.LogWarning($"Unknown cell state '{cell._initialState}' at ({cell._coordinates.x}, {cell._coordinates.y}); falling back to magenta.");
            bg = Color.magenta;
        }

        Texture2D preview = cell._assetToSpawn != null ? AssetPreview.GetAssetPreview(cell._assetToSpawn) : null;

        Background icon = btn.iconImage;
        icon.texture = preview != null ? preview : null;
        btn.iconImage = icon;

        btn.style.backgroundColor = bg;
        btn.style.color = cell._initialState == nameof(DefaultCell) ||
            cell._initialState == nameof(Healing) ||
            cell._initialState == nameof(Empty) ? Color.black : Color.white;
    }
}

#endif