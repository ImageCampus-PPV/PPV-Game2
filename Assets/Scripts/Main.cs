using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using UnityEngine;

public class Main : MonoBehaviour
{
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    private TurnManager _turnManager;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject heavyEngine;
    [SerializeField] private GameObject lightEnemy;
    [SerializeField] private GameObject normalEnemy;

    [SerializeField] private APWalletConfiguration _APWalletConfiguration;
    [SerializeField] private AbilitiesDurationConfiguration _habilitiesDurationConfiguration;
    [SerializeField] private Floor _cellMap;
    [SerializeField] private TerminalConfiguration _terminalConfiguration;

    [SerializeField] private Material _defaultMat;

    private void Awake()
    {
        ServiceProvider.Instance.ClearAllNonPersistanceServices();

        if (_defaultMat == null)
        {
            Debug.LogError("No default material provided");
            return;
        }

        ServiceProvider.Instance.AddService<AbilitiesDurationConfiguration>(_habilitiesDurationConfiguration);
        ServiceProvider.Instance.AddService<AbilitiesDurationConfiguration>(_habilitiesDurationConfiguration);
        ServiceProvider.Instance.AddService<EventBus>(new EventBus());
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid(playerPrefab, heavyEngine, lightEnemy, normalEnemy, _cellMap, _terminalConfiguration, _defaultMat));
        ServiceProvider.Instance.AddService<PathFinding>(new PathFinding());
        ServiceProvider.Instance.AddService<APWallet>(new APWallet(_APWalletConfiguration));
        ServiceProvider.Instance.AddService<EntityRegistry>(new EntityRegistry());
        ServiceProvider.Instance.AddService<AbilitySystem>(new AbilitySystem());
        ServiceProvider.Instance.AddService<CounterSystem>(new CounterSystem());
        ServiceProvider.Instance.AddService<HackSystem>(new HackSystem());

        EventBus.Raise<APRefillEvent>();

        _turnManager = new TurnManager();
        ServiceProvider.Instance.AddService<TurnManager>(_turnManager);

        MapGrid.Init();
        EntityRegistry.Init();
    }

    private void Start()
    {
        _turnManager.Init();
    }

    private void Update()
    {
        _turnManager.Tick();
        if (_turnManager.IsTurnReady && !_turnManager.IsExecuting)
        {
            StartCoroutine(_turnManager.ExecuteTurn());
        }
    }

    private void OnApplicationQuit()
    {
        ServiceProvider.Instance.ClearAllServices();
    }
}

