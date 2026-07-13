using System.Collections;

public abstract class TurnAction
{
    public abstract int APCost { get; }
    public abstract int TickCost { get; }

    public virtual bool CanExecute(Unit unit) => true;

    public abstract IEnumerator Execute(Player player);
}
