using ImageCampus.ToolBox.Events;

internal struct APWalletChangeEvent : IEvent
{
    public uint currentAPAmount;
    public uint maxAPamount;

    public void Assign(params object[] parameters)
    {
        currentAPAmount = (uint)parameters[0];
        maxAPamount = (uint)parameters[1];
    }

    public void Reset()
    {
        currentAPAmount = default(uint);
        maxAPamount = default(uint);
    }
}