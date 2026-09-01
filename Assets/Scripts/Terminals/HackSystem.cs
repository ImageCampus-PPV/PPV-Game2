using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;


public class HackSystem : IService
{
    public bool IsPersistance => false;

    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private TurnManager TurnManager => ServiceProvider.Instance.GetService<TurnManager>();

    private Terminal _activeTerminal;
    public Terminal ActiveTerminal => _activeTerminal;

    public bool CanStartHack(Cell originCell, Terminal terminal, int totalPlannedAPCost)
    {
        if (terminal == null || originCell == null)
            return false;

        if (!terminal.CanBeHacked())
        {
            Debug.Log($"[HackSystem] No se puede hackear: la terminal esta en estado {terminal.EffectiveState} (necesita Active/InProgress/Corrupted).");
            return false;
        }

        if (!TurnManager.IsCellNearUnit(originCell, terminal.Cell, terminal.Range))
        {
            Debug.Log($"[HackSystem] No se puede hackear: la terminal en {terminal.Cell.Coordinates} esta fuera de rango (rango {terminal.Range}) desde {originCell.Coordinates}. Planea un movimiento que te deje adyacente antes de tocar F.");
            return false;
        }

        if (totalPlannedAPCost > APWallet.CurrentAP)
        {
            Debug.Log($"[HackSystem] No se puede hackear: AP insuficiente (necesita {totalPlannedAPCost}, tenes {APWallet.CurrentAP}).");
            return false;
        }

        if (_activeTerminal != null && _activeTerminal != terminal)
        {
            Debug.Log("[HackSystem] No se puede hackear: ya hay otro hackeo en curso este turno.");
            return false;
        }

        return true;
    }

    public void ResolvePlannedHack(Player player, Terminal terminal, int ticksToResolve)
    {
        if (terminal == null || ticksToResolve <= 0)
            return;

        if (player.IsStun || player.CurrentHp == 0)
        {
            terminal.Interrupt();
            return;
        }

        bool isFreshStart = terminal.CurrentTicks == 0;

        if (isFreshStart)
        {
            EventBus.Raise<APConsumeRequestAceptedEvent>(terminal.APCost);
            EventBus.Raise<HackStartedEvent>(terminal.ID, terminal.RequiredTicks);
        }

        _activeTerminal = terminal;

        bool completed = false;

        for (int i = 0; i < ticksToResolve && !completed; i++)
            completed = terminal.AdvanceProgress();

        if (completed)
        {
            Debug.Log($"[HackSystem] Hackeo de {terminal.Type} en {terminal.Cell.Coordinates} completado ({terminal.CurrentTicks}/{terminal.RequiredTicks} ticks).");
            _activeTerminal = null;
        }
        else
        {
            Debug.Log($"[HackSystem] Hackeo de {terminal.Type} en {terminal.Cell.Coordinates} en progreso ({terminal.CurrentTicks}/{terminal.RequiredTicks} ticks). Retomalo el proximo turno.");
        }
    }
}
