using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

// MEC-02 - GDD de Hackeo de Terminales
// Orquesta la accion de "Hackeo" sobre una Terminal: valida condiciones,
// cobra el AP y resuelve el progreso por ticks durante la fase de Execution.
//
// LIMITACION CONOCIDA: SIS-01 (Turnos Simultaneos) todavia no separa
// Planning/Execution en ticks discretos e intercalados con los enemigos (ver
// TurnManager.Tick / EnemiesTurn). Por eso, hoy los ticks de un hackeo se
// resuelven de un tiro dentro de Player.HandleMovement(), en vez de
// intercalarse tick a tick con las acciones enemigas. El costeo en AP y en
// ticks (contra el pool de _maxTicksPerTurn del Player) SI es fiel al GDD, y
// un hackeo que no alcanza a completarse en el budget de ticks del turno
// queda correctamente "En Proceso" para retomar en el siguiente turno.
// Cuando se resuelva SIS-01, ResolvePlannedHack es el lugar a refactorizar
// para que avance de a un tick por llamada real del motor de turnos.
public class HackSystem : IService
{
    public bool IsPersistance => false;

    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private TurnManager TurnManager => ServiceProvider.Instance.GetService<TurnManager>();

    private Terminal _activeTerminal;
    public Terminal ActiveTerminal => _activeTerminal;

    // Condiciones para iniciar un Hackeo (ver GDD):
    // - la terminal esta en un estado hackeable (Active / InProgress / Corrupted)
    // - esta dentro del rango de interaccion
    // - el jugador tiene AP suficiente (incluyendo lo ya reservado en el plan actual)
    // - no hay otro hackeo distinto ya en curso este turno
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

    // Llamado desde Player.HandleMovement() al ejecutar el plan del turno.
    // ticksToResolve es el presupuesto de ticks que el jugador tenia
    // disponible para el hackeo dentro de su _maxTicksPerTurn.
    public void ResolvePlannedHack(Player player, Terminal terminal, int ticksToResolve)
    {
        if (terminal == null || ticksToResolve <= 0)
            return;

        // Condiciones de fallo / interrupcion: si el jugador quedo stuneado o
        // sin vida antes de que el hackeo llegue a resolverse, se interrumpe
        // sin consumir progreso extra.
        if (player.IsStun || player.Life == 0)
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
