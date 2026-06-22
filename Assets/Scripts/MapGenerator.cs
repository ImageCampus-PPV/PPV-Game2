#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class MapGenerator : EditorWindow
{
    private int _rows = 10;
    private int _cols = 10;

    private float _cellSize = 60f;

    private VisualElement _gridContainer;
    private ScrollView _scrollView;

    private IntegerField _rowsField;
    private IntegerField _colsField;

    private Cell[,] _cells;

    private Dictionary<Cell, Button> _cellButtons = new Dictionary<Cell, Button>();

    private const string GridParentName = "MapGrid";
    private GameObject _gridParent;

    private string _prefabSavePath = "Assets/Prefabs/MapGrid.prefab";

    private Dictionary<string, Color> _cellsColorPerState;

    [MenuItem("Tools/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapGenerator>("Map Editor");
    }

    public void CreateGUI()
    {
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

        ObjectField prefabObjectField = new ObjectField("Prefab")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = false
        };
        prefabObjectField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<GameObject>(_prefabSavePath));

        TextField prefabPathField = new TextField("Prefab Path") { value = _prefabSavePath };
        Button savePrefabButton = new Button(SaveGridAsPrefab) { text = "Save Grid As Prefab" };
        Button loadPrefabButton = new Button(LoadGridFromPrefab) { text = "Load Grid From Prefab" };
        Button openPrefabButton = new Button(OpenPrefabForEditing) { text = "Open Prefab" };

        root.Add(_rowsField);
        root.Add(_colsField);
        root.Add(zoomSlider);
        root.Add(rebuildButton);
        root.Add(prefabObjectField);
        root.Add(prefabPathField);
        root.Add(savePrefabButton);
        root.Add(loadPrefabButton);
        root.Add(openPrefabButton);

        _rowsField.RegisterValueChangedCallback(evt => _rows = Mathf.Max(1, evt.newValue));
        _colsField.RegisterValueChangedCallback(evt => _cols = Mathf.Max(1, evt.newValue));

        prefabObjectField.RegisterValueChangedCallback(evt =>
        {
            GameObject droppedAsset = evt.newValue as GameObject;
            if (droppedAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(droppedAsset);
            bool isPrefabAsset = !string.IsNullOrEmpty(path) && path.EndsWith(".prefab");

            if (!isPrefabAsset)
            {
                Debug.LogWarning("Drop a prefab asset from the Project window, not a scene object.");
                prefabObjectField.SetValueWithoutNotify(null);
                return;
            }

            _prefabSavePath = path;
            prefabPathField.SetValueWithoutNotify(_prefabSavePath);
        });

        prefabPathField.RegisterValueChangedCallback(evt =>
        {
            _prefabSavePath = evt.newValue;
            prefabObjectField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<GameObject>(_prefabSavePath));
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

    private void EnsureGridParent()
    {
        if (_gridParent != null)
            return;

        _gridParent = GameObject.Find(GridParentName);
        if (_gridParent == null)
            _gridParent = new GameObject(GridParentName);
    }

    private bool TryValidatePrefabPath()
    {
        if (string.IsNullOrEmpty(_prefabSavePath) || !_prefabSavePath.EndsWith(".prefab"))
        {
            Debug.LogWarning("Prefab path must end with .prefab, e.g. Assets/Prefabs/MapGrid.prefab");
            return false;
        }

        return true;
    }

    private void SaveGridAsPrefab()
    {
        if (_gridParent == null)
        {
            Debug.LogWarning("No grid to save yet — build a grid first.");
            return;
        }

        if (!TryValidatePrefabPath())
            return;

        string directory = System.IO.Path.GetDirectoryName(_prefabSavePath);
        if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(_gridParent, _prefabSavePath, InteractionMode.UserAction);

        Debug.Log($"Saved grid prefab to {_prefabSavePath}");
    }

    private void LoadGridFromPrefab()
    {
        if (!TryValidatePrefabPath())
            return;

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabSavePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"No prefab found at {_prefabSavePath}");
            return;
        }

        GameObject existing = _gridParent != null ? _gridParent : GameObject.Find(GridParentName);
        if (existing != null)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Load Grid From Prefab",
                "This will discard the current grid in the scene and replace it with the saved prefab. Continue?",
                "Load", "Cancel");

            if (!confirmed)
                return;

            DestroyImmediate(existing);
            _gridParent = null;
        }

        _gridParent = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        _gridParent.name = GridParentName;

        if (!RebuildCellsFromHierarchy())
        {
            Debug.LogWarning("Loaded prefab has no recognizable Cell_i_j children; grid is empty.");
        }

        _rowsField?.SetValueWithoutNotify(_rows);
        _colsField?.SetValueWithoutNotify(_cols);

        RebuildGrid();

        Debug.Log($"Loaded grid prefab from {_prefabSavePath}");
    }

    private void OpenPrefabForEditing()
    {
        if (!TryValidatePrefabPath())
            return;

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabSavePath);
        if (prefabAsset == null)
        {
            Debug.LogWarning($"No prefab found at {_prefabSavePath}");
            return;
        }

        // Same as double-clicking the prefab in the Project window — enters Prefab Mode
        // in the Scene view, isolated from the active scene.
        AssetDatabase.OpenAsset(prefabAsset);
    }

    private bool RebuildCellsFromHierarchy()
    {
        Cell[] childCells = _gridParent.GetComponentsInChildren<Cell>(true);

        int maxRow = -1;
        int maxCol = -1;
        List<(int i, int j, Cell cell)> parsed = new List<(int i, int j, Cell cell)>();

        foreach (Cell c in childCells)
        {
            parsed.Add((c.Coordinates.x, c.Coordinates.y, c));
            maxRow = Mathf.Max(maxRow, c.Coordinates.x);
            maxCol = Mathf.Max(maxCol, c.Coordinates.y);
        }

        if (parsed.Count == 0)
        {
            _cells = new Cell[0, 0];
            _rows = 0;
            _cols = 0;
            return false;
        }

        _rows = maxRow + 1;
        _cols = maxCol + 1;

        Cell[,] loadedCells = new Cell[_rows, _cols];
        foreach ((int i, int j, Cell cell) entry in parsed)
        {
            loadedCells[entry.i, entry.j] = entry.cell;
        }

        _cells = loadedCells;
        return true;
    }

    private void RebuildGrid()
    {
        _gridContainer.Clear();

        if (_cells == null)
        {
            EnsureGridParent();

            if (_gridParent.transform.childCount > 0)
                RebuildCellsFromHierarchy();
        }

        bool needsResize = _cells == null ||
                           _cells.GetLength(0) != _rows ||
                           _cells.GetLength(1) != _cols;

        if (needsResize)
        {
            EnsureGridParent();

            Cell[,] oldCells = _cells;
            int oldRows = oldCells?.GetLength(0) ?? 0;
            int oldCols = oldCells?.GetLength(1) ?? 0;

            if (oldCells != null)
            {
                for (int i = 0; i < oldRows; ++i)
                {
                    for (int j = 0; j < oldCols; ++j)
                    {
                        bool outOfBounds = i >= _rows || j >= _cols;
                        if (outOfBounds && oldCells[i, j] != null)
                        {
                            _cellButtons.Remove(oldCells[i, j]);

                            if (Application.isPlaying)
                                Destroy(oldCells[i, j].gameObject);
                            else
                                DestroyImmediate(oldCells[i, j].gameObject);
                        }
                    }
                }
            }

            Cell[,] newCells = new Cell[_rows, _cols];

            for (int i = 0; i < _rows; ++i)
            {
                for (int j = 0; j < _cols; ++j)
                {
                    bool reusable = i < oldRows && j < oldCols && oldCells[i, j] != null;

                    if (reusable)
                    {
                        newCells[i, j] = oldCells[i, j];
                    }
                    else
                    {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = $"Cell_{i}_{j}";
                        go.transform.SetParent(_gridParent.transform);
                        go.transform.position = new Vector3(i * 1.25f, 0.0f, j * 1.25f);
                        newCells[i, j] = go.AddComponent<Cell>();
                        newCells[i, j].SetCoordinate(new Vector2Int(i, j));
                    }
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

                _cellButtons[_cells[i, j]] = cellButton;

                row.Add(cellButton);
            }

            _gridContainer.Add(row);
        }
    }

    private void OnCellClicked(int x, int y)
    {
        CellEditorWindow.ShowWindow(_cells[x, y], OnCellStateChanged);
    }

    private void OnCellStateChanged(Cell changedCell)
    {
        if (_cellButtons.TryGetValue(changedCell, out Button btn))
            ApplyCellVisual(btn, changedCell);
    }

    private void ApplyCellVisual(Button btn, Cell cell)
    {
        Color bg = _cellsColorPerState[cell.InitialState];
        btn.style.backgroundColor = bg;
        btn.style.color = (bg == Color.yellow || bg == Color.white) ? Color.black : Color.white;
    }
}

public class CellEditorWindow : EditorWindow
{
    private Cell cell;
    private Action<Cell> onStateChanged;
    private Dictionary<string, Type> _cellStatesPerName;
    private DropdownField cellTypeDropdown;
    private Label cellInfoLabel;

    public static void ShowWindow(Cell inCell, Action<Cell> onChanged = null)
    {
        CellEditorWindow window = GetWindow<CellEditorWindow>("Cell Editor");
        window.SetCell(inCell, onChanged);
    }

    private void SetCell(Cell inCell, Action<Cell> onChanged)
    {
        cell = inCell;
        onStateChanged = onChanged;
        RefreshGUI();
    }

    private void RefreshGUI()
    {
        if (cell == null || cellTypeDropdown == null)
            return;

        cellTypeDropdown.SetValueWithoutNotify(cell.InitialState);
        cellInfoLabel.text = $"Editing Cell ({cell.Coordinates.x}, {cell.Coordinates.y})";
    }

    private void CreateGUI()
    {
        _cellStatesPerName = new Dictionary<string, Type>();

        foreach (Type type in GetType().Assembly.GetTypes())
        {
            if (type.GetCustomAttribute<CellStateAttribute>() == null)
                continue;

            _cellStatesPerName.Add(type.Name, type);
        }

        List<string> cellStatesNames = new List<string>(_cellStatesPerName.Count);
        foreach (KeyValuePair<string, Type> stateType in _cellStatesPerName)
            cellStatesNames.Add(stateType.Key);

        VisualElement root = rootVisualElement;

        cellInfoLabel = new Label();
        root.Add(cellInfoLabel);

        cellTypeDropdown = new DropdownField(
            label: "Starting State",
            choices: cellStatesNames,
            defaultIndex: 0
        );

        cellTypeDropdown.RegisterValueChangedCallback(evt =>
        {
            if (cell == null)
                return;

            cell.SetInitialState(evt.newValue);
            onStateChanged?.Invoke(cell);
        });

        root.Add(cellTypeDropdown);

        RefreshGUI();
    }

    private void OnGUI()
    {
        if (cell == null)
        {
            EditorGUILayout.LabelField("No cell selected");
            return;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(this);
        }
    }
}

public class CellStateAttribute : Attribute
{
    public float r;
    public float g;
    public float b;
    public float a;
    public CellStateAttribute(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}

#endif