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
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private Button _confirmActionsButton;
    [SerializeField] private TMP_Text _actionTypeText;
    [SerializeField] private TMP_Text _entityTurnText;

    public void Init()
    {
        AssignButtonEvents();
        _actionTypeText.text = "Action selected: ";
        SetCurrentActionText(ClickActionType.Move);

        EventBus.Subscribe<EntityTurnStartEvent>(OnEntityTurnStart);
        _entityTurnText.text = "Turn: Player";
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
        _endTurnButton.onClick.AddListener(() => EventBus.Raise<EndTurnButtonEvent>());
        _confirmActionsButton.onClick.AddListener(() => EventBus.Raise<ConfirmActionsButtonEvent>());

        _moveButton.onClick.AddListener(() => SetCurrentActionText(ClickActionType.Move));
        _hackButton.onClick.AddListener(() => SetCurrentActionText(ClickActionType.Hack));
        _counterButton.onClick.AddListener(() => SetCurrentActionText(ClickActionType.Counter));
        _lagSpikeButton.onClick.AddListener(() => SetCurrentActionText(ClickActionType.LagSpike));
    }

    private void SetCurrentActionText(ClickActionType actionType)
    {
        _actionTypeText.text = "Action selected: ";
        switch (actionType)
        {
            case ClickActionType.Move:
                _actionTypeText.text += "Move";
                break;

            case ClickActionType.Hack:
                _actionTypeText.text += "Hack";
                break;

            case ClickActionType.Counter:
                _actionTypeText.text += "Counter";
                break;

            case ClickActionType.LagSpike:
                _actionTypeText.text += "Lag Spike";
                break;

            default:
                break;
        }
    }

    private void OnEntityTurnStart(in EntityTurnStartEvent callback)
    {
        if (callback.Entity == null)
            return;

        if (callback.Entity is Player)
            _entityTurnText.text = "Turn: Player";
        else
            _entityTurnText.text = $"Turn: {callback.Entity.name}";
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

public struct EndTurnButtonEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct PlayerExecuteActionEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
    }

    public void Reset()
    {
    }
}

public struct EntityTurnStartEvent : IEvent
{
    public Unit Entity;

    public void Assign(params object[] parameters)
    {
        Entity = (Unit)parameters[0];
    }

    public void Reset()
    {
        Entity = null;
    }
}