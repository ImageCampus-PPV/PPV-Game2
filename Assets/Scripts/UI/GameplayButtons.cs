using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ClickActionType
{
    Move,
    Hack,
    Counter,
    LagSpike
}

public class GameplayButtons : MonoBehaviour
{
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    [SerializeField] private Button _moveButton;
    [SerializeField] private Button _hackButton;
    [SerializeField] private Button _counterButton;
    [SerializeField] private Button _lagSpikeButton;
    [SerializeField] private Button _waitButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _undoButton;
    [SerializeField] private Button _breakButton;
    [SerializeField] private Button _confirmActionsButton;
    [SerializeField] private TMP_Text _actionTypeText;

    public void Init()
    {
        AssignButtonEvents();
    }

    private void AssignButtonEvents()
    {
        _moveButton.onClick.AddListener(() => EventBus.Raise<MoveButtonEvent>());
        _hackButton.onClick.AddListener(() => EventBus.Raise<HackButtonEvent>());
        _counterButton.onClick.AddListener(() => EventBus.Raise<CounterButtonEvent>());
        _lagSpikeButton.onClick.AddListener(() => EventBus.Raise<LagSpikeButtonEvent>());
        _waitButton.onClick.AddListener(() => EventBus.Raise<WaitButtonEvent>());
        _restartButton.onClick.AddListener(() => EventBus.Raise<RestartButtonEvent>());
        _undoButton.onClick.AddListener(() => EventBus.Raise<UndoButtonEvent>());
        _breakButton.onClick.AddListener(() => EventBus.Raise<BreakEvent>());
        _confirmActionsButton.onClick.AddListener(() => EventBus.Raise<ConfirmActionsButtonEvent>());
    }
}
public struct MoveButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct HackButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct CounterButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct LagSpikeButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct WaitButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct RestartButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct UndoButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct ConfirmActionsButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}