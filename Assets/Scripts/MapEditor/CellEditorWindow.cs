#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CellEditorWindow : EditorWindow
{
    private CellData cell;
    private Action<CellData> onStateChanged;
    private Dictionary<string, Type> _cellStatesPerName;
    private DropdownField cellTypeDropdown;
    private DropdownField unitSpawnType;
    private Toggle playerCanSpawnThereField;
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

        VisualElement root = rootVisualElement;

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

        playerCanSpawnThereField = new Toggle("Player Spawn Point");

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

        root.Add(unitSpawnType);
        root.Add(cellTypeDropdown);

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