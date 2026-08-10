using ImageCampus.ToolBox.Events;

public struct HackCompletedEvent : IEvent
{
    public uint terminalId;
    public TerminalType terminalType;

    public void Assign(params object[] parameters)
    {
        terminalId = (uint)parameters[0];
        terminalType = (TerminalType)parameters[1];
    }

    public void Reset()
    {
        terminalId = default(uint);
        terminalType = default(TerminalType);
    }
}
