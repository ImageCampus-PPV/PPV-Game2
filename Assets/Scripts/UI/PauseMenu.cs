using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button _resumeGameButton;
    [SerializeField] private Button _exitToMenuButton;
    [SerializeField] private Button _restartGameButton;

    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private void Awake()
    {
        _resumeGameButton.onClick.AddListener(OnResumeGame);
        _exitToMenuButton.onClick.AddListener(OnExitToMenu);
        _restartGameButton.onClick.AddListener(OnRestartGame);
    }

    private void OnRestartGame()
    {
        EventBus.Raise<ResetLevelEvent>();
    }

    private void OnExitToMenu()
    {
        EventBus.Raise<BackToMenuEvent>();
    }

    private void OnResumeGame()
    {
        EventBus.Raise<ResumeGameEvent>();
    }
}

public struct ResetLevelEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}