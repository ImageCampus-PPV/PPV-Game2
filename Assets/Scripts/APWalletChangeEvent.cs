using ImageCampus.ToolBox.Events;

internal struct APWalletChangeEvent : IEvent
{
    public int currentAPAmount;
    public int maxAPamount;

    public void Assign(params object[] parameters)
    {
        currentAPAmount = (int)parameters[0];
        maxAPamount = (int)parameters[1];
    }

    public void Reset()
    {
        currentAPAmount = default(int);
        maxAPamount = default(int);
    }
}