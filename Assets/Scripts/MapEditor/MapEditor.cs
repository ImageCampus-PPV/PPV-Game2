#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class MapEditor : EditorWindow
{
    private string _cellsMapSavePath = "Assets/Maps/CellsMap.asset";
    private Map _cellsMapAsset = null;

    [SerializeField]
    private List<Floor> _floors;

    private SerializedObject serializedObj;
    private SerializedProperty serializedProp;

    private PropertyField _floorsField;

    [MenuItem("Tools/Map Editor")]
    public static void ShowWindow()
    {
        GetWindow<MapEditor>("Map Editor");
    }

    // OnEnable runs before any GUI callback and survives domain reloads,
    // so this is where serializedObj/_floors must be initialized —
    // not CreateGUI, which can run after the first GUI pass.
    private void OnEnable()
    {
        if (_floors == null)
            _floors = new List<Floor>();

        serializedObj = new SerializedObject(this);
        serializedProp = serializedObj.FindProperty(nameof(_floors));
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        Label header = new Label("Manage Your Elements List");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginTop = 10;
        header.style.marginBottom = 5;
        root.Add(header);

        _floorsField = new PropertyField(serializedProp);
        _floorsField.Bind(serializedObj);
        root.Add(_floorsField);

        TextField cellsMapPathField = new TextField("Cells Map Path") { value = _cellsMapSavePath };
        cellsMapPathField.RegisterValueChangedCallback(evt => _cellsMapSavePath = evt.newValue);

        Button saveCellsMapButton = new Button(SaveGridAsCellsMap) { text = "Save Grid As Cells Map" };
        Button loadCellsMapButton = new Button(LoadGridFromCellsMap) { text = "Load Grid From Cells Map" };

        ObjectField cellsMapObjectField = new ObjectField("Map Asset")
        {
            objectType = typeof(Map),
            allowSceneObjects = false
        };

        cellsMapObjectField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<Map>(_cellsMapSavePath));

        cellsMapObjectField.RegisterValueChangedCallback(evt =>
        {
            Map droppedAsset = evt.newValue as Map;
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

            ApplyLoadedFloors(droppedAsset);
        });

        root.Add(cellsMapObjectField);
        root.Add(cellsMapPathField);
        root.Add(saveCellsMapButton);
        root.Add(loadCellsMapButton);
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

        Map asset = AssetDatabase.LoadAssetAtPath<Map>(_cellsMapSavePath);
        bool isNewAsset = asset == null;

        if (isNewAsset)
        {
            asset = ScriptableObject.CreateInstance<Map>();
        }

        asset._floors = _floors.ToArray();

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

        Map asset = AssetDatabase.LoadAssetAtPath<Map>(_cellsMapSavePath);
        if (asset == null)
        {
            Debug.LogWarning($"No CellsMaps asset found at {_cellsMapSavePath}");
            return;
        }

        _cellsMapAsset = asset;
        ApplyLoadedFloors(asset);

        Debug.Log($"Loaded grid from {_cellsMapSavePath}");
    }

    private void ApplyLoadedFloors(Map asset)
    {
        _floors = asset._floors != null
            ? new List<Floor>(asset._floors)
            : new List<Floor>();

        serializedObj.Update();

        serializedProp = serializedObj.FindProperty(nameof(_floors));
        if (_floorsField != null)
        {
            _floorsField.Unbind();
            _floorsField.bindingPath = serializedProp.propertyPath;
            _floorsField.Bind(serializedObj);
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
}
#endif