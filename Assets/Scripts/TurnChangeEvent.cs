using ImageCampus.ToolBox.Events;

public struct TurnChangeEvent : IEvent
{
    public uint currentTurn;

    public void Assign(params object[] parameters)
    {
        currentTurn = (uint)parameters[0];
    }

    public void Reset()
    {
        currentTurn = default(uint);
    }
}