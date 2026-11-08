using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using Systems.SceneLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private GameObject _floatingTextGO;
    [SerializeField] private GameObject _hudPrefab;
    [SerializeField] private GameObject _pausedScreenPrefab;

    [SerializeField] private SceneRef _nextLevel;
    [SerializeField] private SceneRef _menuScene;

    private bool _isPaused = false;
    private GameObject _pausedScreenGO;
    private GameObject _hudGO;
    private static int i = 0;

    private void Awake()
    {
        _pausedScreenGO = Instantiate(_pausedScreenPrefab);
        _hudGO = Instantiate(_hudPrefab);

        SetPaused(false);

        ServiceProvider.Instance.ClearAllNonPersistanceServices();
        gameObject.transform.position = Vector3.zero;
        gameObject.transform.localScale = Vector3.one;

        if (_defaultMat == null)
        {
            Debug.LogError("No default material provided");
            return;
        }

        ServiceProvider.Instance.AddService<AbilitiesDurationConfiguration>(_abilitiesDurationConfiguration);
        ServiceProvider.Instance.AddService<EventBus>(new EventBus());
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid(gameObject.AddComponent<Grid>(), _playerPrefab, _heavyEnemy, _agileEnemy, _normalEnemy, _cellMap, _terminalConfiguration, _defaultMat));
        ServiceProvider.Instance.AddService<PathFinding>(new PathFinding());
        ServiceProvider.Instance.AddService<APWallet>(new APWallet(_APWalletConfiguration));
        ServiceProvider.Instance.AddService<EntityRegistry>(new EntityRegistry());
        ServiceProvider.Instance.AddService<AbilitySystem>(new AbilitySystem());
        ServiceProvider.Instance.AddService<PushSystem>(new PushSystem());
        ServiceProvider.Instance.AddService<HackSystem>(new HackSystem());
        ServiceProvider.Instance.AddService<TileHoverHighlighter>(new TileHoverHighlighter(Camera.main));
        ServiceProvider.Instance.AddService<FloatingTextInstancer>(new FloatingTextInstancer(_floatingTextGO, _hudGO));

        EventBus.Raise<APRefillEvent>();

        _turnManager = new TurnManager();
        ServiceProvider.Instance.AddService<TurnManager>(_turnManager);

        MapGrid.Init();
        EntityRegistry.Init();
        _hudGO.GetComponent<HudManager>().Init();

        EventBus.Subscribe<NextLevelEvent>(OnNextLevel);
        EventBus.Subscribe<ResumeGameEvent>(ResumeGame);
        EventBus.Subscribe<ResetLevelEvent>(RestartGame);
        EventBus.Subscribe<BackToMenuEvent>(ExitToMenu);
        EventBus.Subscribe<PauseEvent>(PauseGame);
    }

    private void RestartGame(in ResetLevelEvent callback)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PauseGame(in PauseEvent callback)
    {
        SetPaused(!_isPaused);
    }

    private void SetPaused(bool isPaused)
    {
        _pausedScreenGO.SetActive(isPaused);
        _isPaused = isPaused;
    }

    private void ResumeGame(in ResumeGameEvent resumeGameEvent)
    {
        SetPaused(false);
    }

    private void ExitToMenu(in BackToMenuEvent backToMenuEvent)
    {
        SetPaused(false);
        SceneManager.LoadScene(_menuScene.Index);
    }

    private void OnNextLevel(in NextLevelEvent callback)
    {
        ++i;
        SceneManager.LoadScene(_nextLevel.Index);
    }

    private void Start()
    {
        _turnManager.Init();
    }

    private void Update()
    {
        if (_isPaused)
            return;

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


public struct ResumeGameEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct BackToMenuEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {   
    }
}

public struct PauseEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}