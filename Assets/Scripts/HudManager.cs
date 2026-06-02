using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HudManager : MonoBehaviour
{
    [SerializeField] private Button _exitButton;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _APText;
    [SerializeField] private TMP_Text _playerLife;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private void Awake()
    {
        _exitButton.onClick.AddListener(ExitProgram);
    }

    private void Start()
    {
        EventBus.Subscribe<TurnChangeEvent>(OnTurnChange);
        EventBus.Subscribe<APWalletChangeEvent>(OnAPChange);
        EventBus.Subscribe<PlayerChangeLifeEvent>(OnPlayerLifeChange);
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
        _APText.text = $"AP: {APWalletChangeEvent.currentAPAmount} / {APWalletChangeEvent.maxAPamount}.";
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
