using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class Main : MonoBehaviour
{
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    private TileHoverHighlighter TileHoverHighlighter => ServiceProvider.Instance.GetService<TileHoverHighlighter>();
    private TurnManager _turnManager;

    [Header("Entities")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _heavyEnemy;
    [SerializeField] private GameObject _agileEnemy;
    [SerializeField] private GameObject _normalEnemy;

    [Header("Configurations")]
    [SerializeField] private APWalletConfiguration _APWalletConfiguration;
    [SerializeField] private AbilitiesDurationConfiguration _abilitiesDurationConfiguration;
    [SerializeField] private Floor _cellMap;
    [SerializeField] private TerminalConfiguration _terminalConfiguration;

    [Header("UI Elements")]
    [SerializeField] private Material _defaultMat;
    [SerializeField] private GameplayButtons _UiButtonsScript;
    [SerializeField] private GameObject _floatingTextGO;
    [SerializeField] private GameObject _canvasGO;

    private void Awake()
    {
        ServiceProvider.Instance.ClearAllNonPersistanceServices();

        if (_defaultMat == null)
        {
            Debug.LogError("No default material provided");
            return;
        }

        ServiceProvider.Instance.AddService<AbilitiesDurationConfiguration>(_abilitiesDurationConfiguration);
        ServiceProvider.Instance.AddService<EventBus>(new EventBus());
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid(_playerPrefab, _heavyEnemy, _agileEnemy, _normalEnemy, _cellMap, _terminalConfiguration, _defaultMat));
        ServiceProvider.Instance.AddService<PathFinding>(new PathFinding());
        ServiceProvider.Instance.AddService<APWallet>(new APWallet(_APWalletConfiguration));
        ServiceProvider.Instance.AddService<EntityRegistry>(new EntityRegistry());
        ServiceProvider.Instance.AddService<AbilitySystem>(new AbilitySystem());
        ServiceProvider.Instance.AddService<KickSystem>(new KickSystem());
        ServiceProvider.Instance.AddService<HackSystem>(new HackSystem());
        ServiceProvider.Instance.AddService<TileHoverHighlighter>(new TileHoverHighlighter(Camera.main));
        ServiceProvider.Instance.AddService<FloatingTextInstancer>(new FloatingTextInstancer(_floatingTextGO, _canvasGO));

        EventBus.Raise<APRefillEvent>();

        _turnManager = new TurnManager();
        ServiceProvider.Instance.AddService<TurnManager>(_turnManager);

        MapGrid.Init();
        _UiButtonsScript.Init();
        EntityRegistry.Init();
    }

    private void Start()
    {
        _turnManager.Init();
    }

    private void Update()
    {
        TileHoverHighlighter.Tick();

        _turnManager.Tick();

        if (_turnManager.ShouldExecutePlayerAction && !_turnManager.IsExecuting)
            StartCoroutine(_turnManager.ExecutePlayerActions());

        if (_turnManager.IsTurnReady && !_turnManager.IsExecuting)
            StartCoroutine(_turnManager.ExecuteEnemiesTurn());
    }

    private void OnApplicationQuit()
    {
        ServiceProvider.Instance.ClearAllServices();
    }
}
