using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HudManager : MonoBehaviour
{
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _APText;
    [SerializeField] private TMP_Text _playerLife;
    [SerializeField] private GameObject _toolsGO;
    [SerializeField] private GameObject _successBgGO;
    [SerializeField] private GameObject _failedBgGO;
    [SerializeField] private GameplayButtons _gameplayButtons;

    [Header("End level UI")]
    [SerializeField] private float _endLevelDelay = 2f;
    [SerializeField] private GameObject _endTextGO;
    [SerializeField] private TextMeshProUGUI _endText;
    [SerializeField] private bool _isLastLevel = false;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    public void Init()
    {
        if (_endText == null || _endTextGO == null)
        {
            Debug.LogError("No end text or GO provided");
            return;
        }

        _gameplayButtons.Init();

        _restartButton.onClick.AddListener(ResetButtonClicked);
        _pauseButton.onClick.AddListener(OnPause);
        _toolsGO.SetActive(false);
        _endTextGO.SetActive(false);
        _successBgGO.SetActive(false);
        _failedBgGO.SetActive(false);
    }

    private void OnPause()
    {
        EventBus.Raise<PauseEvent>();
    }

    private void Start()
    {
        EventBus.Subscribe<TurnChangeEvent>(OnTurnChange);
        EventBus.Subscribe<APWalletChangeEvent>(OnAPChange);
        EventBus.Subscribe<PlayerChangeLifeEvent>(OnPlayerLifeChange);
        EventBus.Subscribe<LevelCompleteEvent>(OnWin);
        EventBus.Subscribe<LevelFailedEvent>(OnLoss);
        EventBus.Subscribe<ToggleToolsEvent>(OnToggleTools);
    }

    private void OnToggleTools(in ToggleToolsEvent callback)
    {
        _toolsGO.SetActive(!(_toolsGO.activeSelf));
    }

    private void OnLoss(in LevelFailedEvent callback)
    {
        _successBgGO.SetActive(false);
        _failedBgGO.SetActive(true);
        EndLevel("FAILED");
    }

    private void OnWin(in LevelCompleteEvent callback)
    {
        _successBgGO.SetActive(true);
        _failedBgGO.SetActive(false);
        NextLevel();
    }

    private void EndLevel(string endMessage)
    {
        _endText.text = endMessage;
        _endTextGO.SetActive(true);
        StartCoroutine(ResetLevel());
    }
    private void NextLevel()
    {
        _endText.text = "SUCCESS";
        _endTextGO.SetActive(true);
        StartCoroutine(GoToNextLevel());
    }

    private void ResetButtonClicked()
    {
        EndLevel("Restarting");
    }

    //TODO: Use our scene manager (when available)
    private IEnumerator ResetLevel()
    {
        yield return new WaitForSeconds(_endLevelDelay);
        EventBus.Raise<ResetLevelEvent>();
        yield break;
    }

    private IEnumerator GoToNextLevel()
    {
        yield return new WaitForSeconds(_endLevelDelay);
        EventBus.Raise<NextLevelEvent>();
        yield break;
    }

    private void OnPlayerLifeChange(in PlayerChangeLifeEvent playerChangeLifeEvent)
    {
        _playerLife.text = $"Life: {playerChangeLifeEvent.currentLife}";
    }

    private void OnTurnChange(in TurnChangeEvent turnChangeEvent)
    {
        _turnText.text = $"Turn: {turnChangeEvent.currentTurn}.";
    }

    private void OnAPChange(in APWalletChangeEvent APWalletChangeEvent)
    {
        int currentAPToShow = APWalletChangeEvent.currentAPAmount;

        if (currentAPToShow < 0)
            currentAPToShow = 0;

        _APText.text = $"AP: {currentAPToShow} / {APWalletChangeEvent.maxAPamount}.";
    }

    private void ExitProgram()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}


public struct NextLevelEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}