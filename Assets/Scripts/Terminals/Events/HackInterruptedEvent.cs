using ImageCampus.ToolBox.Events;

public struct HackInterruptedEvent : IEvent
{
    public uint terminalId;
    public int progressKept;

    public void Assign(params object[] parameters)
    {
        terminalId = (uint)parameters[0];
        progressKept = (int)parameters[1];
    }

    public void Reset()
    {
        terminalId = default(uint);
        progressKept = default(int);
    }
}
