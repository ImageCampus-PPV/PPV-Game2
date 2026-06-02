using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HudManager : MonoBehaviour
{
    [SerializeField] private Button _exitButton;
    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _APText;

    EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private void Awake()
    {
        _exitButton.onClick.AddListener(ExitProgram);
    }

    private void Start()
    {
        EventBus.Subscribe<TurnChangeEvent>(OnTurnChange);
        EventBus.Subscribe<APWalletChangeEvent>(OnAPChange);
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
