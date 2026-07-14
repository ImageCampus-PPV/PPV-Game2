#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class CellEditorWindow : EditorWindow
{
    private CellData cell;
    private Action<CellData> onStateChanged;
    private Dictionary<string, Type> _cellStatesPerName;
    private DropdownField cellTypeDropdown;
    private DropdownField unitSpawnType;
    private DropdownField terminalTypeDropdown;
    private Label cellInfoLabel;

    public static void ShowWindow(CellData inCell, Action<CellData> onChanged = null)
    {
        CellEditorWindow window = GetWindow<CellEditorWindow>("Cell Editor");
        window.SetCell(inCell, onChanged);
    }

    private void SetCell(CellData inCell, Action<CellData> onChanged)
    {
        cell = inCell;
        onStateChanged = onChanged;
        RefreshGUI();
    }

    private void RefreshGUI()
    {
        if (cellTypeDropdown == null || cellTypeDropdown == null)
            return;

        unitSpawnType.SetValueWithoutNotify(cell._spawnUnit);
        cellTypeDropdown.SetValueWithoutNotify(cell._initialState);
        terminalTypeDropdown?.SetValueWithoutNotify(string.IsNullOrEmpty(cell._terminalType) ? "None" : cell._terminalType);
        cellInfoLabel.text = $"Editing Cell ({cell._coordinates.x}, {cell._coordinates.y})";
    }

    private void CreateGUI()
    {
        #region CellStates
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
        #endregion

        List<string> unitTypeNames = new List<string>();
        unitTypeNames.Add("None");
        foreach (Type type in GetType().Assembly.GetTypes())
        {
            if (!typeof(Unit).IsAssignableFrom(type) || type.IsAbstract)
                continue;

            unitTypeNames.Add(type.Name);
        }

        List<string> terminalTypeNames = new List<string>();
        terminalTypeNames.Add("None");
        terminalTypeNames.AddRange(Enum.GetNames(typeof(TerminalType)));

        VisualElement root = rootVisualElement;

        ObjectField spawnDecorationField = new ObjectField("Decoration")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = false
        };

        spawnDecorationField.RegisterValueChangedCallback(evt =>
        {
            GameObject gameObject = evt.newValue as GameObject;
            bool isPrefabAsset = gameObject != null
                && PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab;

            if (!isPrefabAsset)
            {
                Debug.LogWarning("Drop a Prefab asset from the Project window, not a scene object or non-prefab asset.");
                spawnDecorationField.SetValueWithoutNotify(null);
                gameObject = null;
            }

            cell._assetToSpawn = gameObject;
            onStateChanged?.Invoke(cell);
        });

        cellInfoLabel = new Label();
        root.Add(cellInfoLabel);

        unitSpawnType = new DropdownField(
            label: "Entity to Spawn",
            choices: unitTypeNames,
            defaultIndex: 0
        );

        cellTypeDropdown = new DropdownField(
            label: "Starting State",
            choices: cellStatesNames,
            defaultIndex: 0
        );

        terminalTypeDropdown = new DropdownField(
            label: "Terminal (MEC-02)",
            choices: terminalTypeNames,
            defaultIndex: 0
        );

        unitSpawnType.RegisterValueChangedCallback(evt =>
        {
            cell._spawnUnit = evt.newValue;
            onStateChanged?.Invoke(cell);
        });

        cellTypeDropdown.RegisterValueChangedCallback(evt =>
        {
            cell._initialState = evt.newValue;
            onStateChanged?.Invoke(cell);
        });

        terminalTypeDropdown.RegisterValueChangedCallback(evt =>
        {
            cell._terminalType = evt.newValue;
            onStateChanged?.Invoke(cell);
        });

        root.Add(unitSpawnType);
        root.Add(cellTypeDropdown);
        root.Add(terminalTypeDropdown);
        root.Add(spawnDecorationField);

        RefreshGUI();
    }

    private void OnGUI()
    {
        if (GUI.changed)
        {
            EditorUtility.SetDirty(this);
        }
    }
}

#endif