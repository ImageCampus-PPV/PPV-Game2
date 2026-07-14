using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class Main : MonoBehaviour
{
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    private EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    private TurnManager _turnManager;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject heavyEngine;
    [SerializeField] private GameObject lightEnemy;
    [SerializeField] private GameObject normalEnemy;

    [SerializeField] private APWalletConfiguration _APWalletConfiguration;
    [SerializeField] private HabilitiesDurationConfiguration _habilitiesDurationConfiguration;
    [SerializeField] private Floor _cellMap;
    [SerializeField] private TerminalConfiguration _terminalConfiguration;

    private void Awake()
    {
        ServiceProvider.Instance.AddService<HabilitiesDurationConfiguration>(_habilitiesDurationConfiguration);
        ServiceProvider.Instance.AddService<HabilitiesDurationConfiguration>(_habilitiesDurationConfiguration);
        ServiceProvider.Instance.AddService<EventBus>(new EventBus());
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid(playerPrefab, heavyEngine, lightEnemy, normalEnemy, _cellMap, _terminalConfiguration));
        ServiceProvider.Instance.AddService<PathFinding>(new PathFinding());
        ServiceProvider.Instance.AddService<APWallet>(new APWallet(_APWalletConfiguration));
        ServiceProvider.Instance.AddService<EntityRegistry>(new EntityRegistry());
        ServiceProvider.Instance.AddService<AbilitySystem>(new AbilitySystem());
        ServiceProvider.Instance.AddService<CounterSystem>(new CounterSystem());
        ServiceProvider.Instance.AddService<HackSystem>(new HackSystem());

      
        ServiceProvider.Instance.GetService<APWallet>().Init();

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
    }

    private void OnApplicationQuit()
    {
        ServiceProvider.Instance.ClearAllServices();
    }
}
