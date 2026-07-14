// MEC-02 - GDD de Hackeo de Terminales
// Estados de una terminal (seccion "Estados de Terminal" / "Reglas de estado" del GDD).
public enum TerminalState
{
    Inactive,   // No puede ser utilizada todavia.
    Active,     // Puede ser hackeada si el jugador cumple con las condiciones.
    Blocked,    // Existe una condicion / situacion que impide hackearla.
    Corrupted,  // Esta afectada por la corrupcion de la IDOL (riesgo extra, pero sigue siendo hackeable).
    InProgress, // El hackeo fue iniciado pero no completado.
    Completed,  // El hackeo fue resuelto y produjo su efecto.
}
