using ImageCampus.ToolBox.Events;

public struct HackStartedEvent : IEvent
{
    public uint terminalId;
    public int requiredTicks;

    public void Assign(params object[] parameters)
    {
        terminalId = (uint)parameters[0];
        requiredTicks = (int)parameters[1];
    }

    public void Reset()
    {
        terminalId = default(uint);
        requiredTicks = default(int);
    }
}
