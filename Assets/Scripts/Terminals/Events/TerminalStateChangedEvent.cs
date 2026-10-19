using ImageCampus.ToolBox.Events;

public struct TerminalStateChangedEvent : IEvent
{
    public uint terminalId;
    public TerminalState newState;

    public void Assign(params object[] parameters)
    {
        terminalId = (uint)parameters[0];
        newState = (TerminalState)parameters[1];
    }

    public void Reset()
    {
        terminalId = default(uint);
        newState = default(TerminalState);
    }
}
