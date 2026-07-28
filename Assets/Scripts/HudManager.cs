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
    [SerializeField] private Button _exitButton;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _APText;
    [SerializeField] private TMP_Text _playerLife;

    [Header("End level UI")]
    [SerializeField] private float _endLevelDelay = 2f;
    [SerializeField] private GameObject _endTextGO;
    [SerializeField] private TextMeshProUGUI _endText;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private void Awake()
    {
        if (_exitButton == null)
        {
            Debug.LogError("No exit button provided");
            return;
        }

        if (_endText == null || _endTextGO == null)
        {
            Debug.LogError("No end text or GO provided");
            return;
        }

        _exitButton.onClick.AddListener(ExitProgram);
        _endTextGO.SetActive(false);
    }

    private void Start()
    {
        EventBus.Subscribe<TurnChangeEvent>(OnTurnChange);
        EventBus.Subscribe<APWalletChangeEvent>(OnAPChange);
        EventBus.Subscribe<PlayerChangeLifeEvent>(OnPlayerLifeChange);
        EventBus.Subscribe<LevelCompleteEvent>(OnWin);
        EventBus.Subscribe<LevelFailedEvent>(OnLoss);
    }

    private void OnLoss(in LevelFailedEvent callback)
    {
        EndLevel("You LOST!");
    }

    private void OnWin(in LevelCompleteEvent callback)
    {
        EndLevel("You WON!");
    }

    private void EndLevel(string endMessage)
    {
        _endText.text = endMessage;
        _endTextGO.SetActive(true);
        StartCoroutine(ResetLevel());
    }

    //TODO: Use our scene manager (when available)
    private IEnumerator ResetLevel()
    {
        yield return new WaitForSeconds(_endLevelDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
