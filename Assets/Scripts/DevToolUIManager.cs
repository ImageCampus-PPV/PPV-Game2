using ImageCampus.ToolBox.Services;
using ImageCampus.ToolBox.Events;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;

public class DevToolUIManager : MonoBehaviour
{
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    [Header("Containers")]
    [SerializeField] private GameObject _allUIGO;
    [SerializeField] private GameObject _gridSection;
    [SerializeField] private GameObject _enemySection;
    [SerializeField] private GameObject _playerSection;
    [SerializeField] private GameObject _apSection;

    [Header("Grid")]
    [SerializeField] private TMP_InputField _cellX;
    [SerializeField] private TMP_InputField _cellY;
    [SerializeField] private TMP_Dropdown _stateDropdown;

    [Header("Entities")]
    [SerializeField] private TMP_InputField _enemyX;
    [SerializeField] private TMP_InputField _enemyY;
    [SerializeField] private TMP_Dropdown _enemyDropdown;
    [SerializeField] private TMP_InputField _removeX;
    [SerializeField] private TMP_InputField _removeY;

    [Header("Player")]
    [SerializeField] private TMP_InputField _playerX;
    [SerializeField] private TMP_InputField _playerY;

    [Header("AP")]
    [SerializeField] private TMP_InputField _ap;

    [Header("Buttons")]
    [SerializeField] private Button _gridActivationButton;
    [SerializeField] private Button _enemyActivationButton;
    [SerializeField] private Button _playerActivationButton;
    [SerializeField] private Button _APActivationButton;
    [SerializeField] private Button _activationButton;
    [SerializeField] private Button _applyCellStateButton;
    [SerializeField] private Button _spawnEnemyButton;
    [SerializeField] private Button _removeEntityButton;
    [SerializeField] private Button _movePlayerButton;
    [SerializeField] private Button _setAPButton;

    private readonly List<Button> _activationButtons = new();

    private void Awake()
    {
        LoadDropdowns();

        _activationButtons.Add(_gridActivationButton);
        _activationButtons.Add(_enemyActivationButton);
        _activationButtons.Add(_playerActivationButton);
        _activationButtons.Add(_APActivationButton);

        _activationButton.onClick.AddListener(() => _allUIGO.SetActive(!_allUIGO.activeSelf));

        _gridActivationButton.onClick.AddListener(() => ToggleSection(_gridSection, _gridActivationButton));
        _enemyActivationButton.onClick.AddListener(() => ToggleSection(_enemySection, _enemyActivationButton));
        _playerActivationButton.onClick.AddListener(() => ToggleSection(_playerSection, _playerActivationButton));
        _APActivationButton.onClick.AddListener(() => ToggleSection(_apSection, _APActivationButton));

        _applyCellStateButton.onClick.AddListener(ApplyCellState);
        _spawnEnemyButton.onClick.AddListener(SpawnEnemy);
        _removeEntityButton.onClick.AddListener(RemoveEntity);
        _movePlayerButton.onClick.AddListener(MovePlayer);
        _setAPButton.onClick.AddListener(SetAP);
    }

    private void ToggleSection(GameObject section, Button activator)
    {
        bool isActive = !section.activeSelf;

        section.SetActive(isActive);

        foreach (Button button in _activationButtons)
            button.gameObject.SetActive(!isActive);

        activator.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        _applyCellStateButton.onClick.RemoveListener(ApplyCellState);
        _spawnEnemyButton.onClick.RemoveListener(SpawnEnemy);
        _removeEntityButton.onClick.RemoveListener(RemoveEntity);
        _movePlayerButton.onClick.RemoveListener(MovePlayer);
        _setAPButton.onClick.RemoveListener(SetAP);
    }

    private void LoadDropdowns()
    {
        _stateDropdown.ClearOptions();
        _stateDropdown.AddOptions(GetCellStateNames());

        _enemyDropdown.ClearOptions();
        _enemyDropdown.AddOptions(new List<string>
        {
            "HeavyEnemy",
            "LightEnemy",
            "NormalEnemy"
        });
    }

    private List<string> GetCellStateNames()
    {
        List<string> names = new();

        foreach (Type type in typeof(MapGrid).Assembly.GetTypes())
        {
            if (typeof(State).IsAssignableFrom(type) &&
                type.GetCustomAttribute<CellStateAttribute>() != null)
            {
                names.Add(type.Name);
            }
        }

        return names;
    }

    private int IntValue(TMP_InputField field)
    {
        int.TryParse(field.text, out int value);
        return value;
    }

    public void ApplyCellState()
    {
        EventBus.Raise<DevChangeCellStateEvent>(
            IntValue(_cellX),
            IntValue(_cellY),
            _stateDropdown.options[_stateDropdown.value].text);
    }

    public void SpawnEnemy()
    {
        EventBus.Raise<DevSpawnEnemyEvent>(
            _enemyDropdown.options[_enemyDropdown.value].text,
            IntValue(_enemyX),
            IntValue(_enemyY));
    }

    public void RemoveEntity()
    {
        EventBus.Raise<DevRemoveEntityAtCellEvent>(
            IntValue(_removeX),
            IntValue(_removeY));
    }

    public void MovePlayer()
    {
        EventBus.Raise<DevMovePlayerEvent>(
            IntValue(_playerX),
            IntValue(_playerY));
    }

    public void SetAP()
    {
        EventBus.Raise<DevSetAPEvent>(
            IntValue(_ap));
    }
}