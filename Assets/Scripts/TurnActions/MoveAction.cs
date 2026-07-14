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
    public Cell OriginCell => _originCell;

    public override IEnumerator Execute(Unit unit)
    {
        yield return unit.MoveTo(_targetCell);

        AdvanceTick();
    }
}