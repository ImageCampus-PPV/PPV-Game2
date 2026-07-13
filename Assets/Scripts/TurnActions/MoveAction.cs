using System.Collections;

public class MoveAction : TurnAction
{
    private readonly Cell _targetCell;
    private readonly Cell _originCell;

    public MoveAction(Cell originCell, Cell targetCell)
    {
        _originCell = originCell;
        _targetCell = targetCell;
    }

    public override int APCost => 1;
    public override int TickCost => 1;

    public Cell TargetCell => _targetCell;
    public Cell OriginCell => _originCell;

    public override IEnumerator Execute(Player player)
    {
        yield return player.MoveTo(_targetCell);
    }
}