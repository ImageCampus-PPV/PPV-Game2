// MEC-02 - GDD de Hackeo de Terminales
// Tipos de terminal descriptos en la seccion "Elementos" del GDD.
public enum TerminalType
{
    Access,          // Terminal de acceso: abre puertas / rutas bloqueadas.
    Influence,       // Terminal de influencia: reduce tiles de corrupcion / influencia de la IDOL.
    Purification,    // Terminal de purificacion: limpia corrupcion del piso.
    Combat,          // Terminal de combate: afecta enemigos (stun, reduccion de dano, movimiento).
    Reward,          // Terminal de recompensa: otorga buff o recurso.
    FloorObjective,  // Terminal de objetivo de piso: vinculada a la condicion principal de avance.
}
