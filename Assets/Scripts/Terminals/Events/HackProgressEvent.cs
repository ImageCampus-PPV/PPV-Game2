using ImageCampus.ToolBox.Events;

public struct HackProgressEvent : IEvent
{
    public uint terminalId;
    public int currentTicks;
    public int requiredTicks;

    public void Assign(params object[] parameters)
    {
        terminalId = (uint)parameters[0];
        currentTicks = (int)parameters[1];
        requiredTicks = (int)parameters[2];
    }

    public void Reset()
    {
        terminalId = default(uint);
        currentTicks = default(int);
        requiredTicks = default(int);
    }
}
