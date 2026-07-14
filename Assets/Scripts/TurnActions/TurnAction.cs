using System.Collections;

public abstract class TurnAction
{
    public int TotalTicks { get; }
    public int APCost { get; }
    public int CurrentTick { get; private set; }
    public bool IsFinished => CurrentTick >= TotalTicks;

    protected TurnAction(int totalTicks, int APCost)
    {
        TotalTicks = totalTicks;
        this.APCost = APCost;
    }

    protected void AdvanceTick()
    {
        CurrentTick++;
    }

    public abstract IEnumerator Execute(Player player);
}