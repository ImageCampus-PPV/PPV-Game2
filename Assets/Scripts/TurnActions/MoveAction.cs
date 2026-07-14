using System.Collections;

public class MoveAction : TurnAction
{
    private readonly Cell _originCell;
    private readonly Cell _targetCell;

    public MoveAction(Cell originCell, Cell targetCell, int apCost) : base(apCost, 1)
    {
        _originCell = originCell;
        _targetCell = targetCell;
    }

    public Cell TargetCell => _targetCell;

    public override IEnumerator Execute(Player player)
    {
        yield return player.MoveTo(_targetCell);

        AdvanceTick();
    }
}