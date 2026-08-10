using System.Collections;
using UnityEngine;

public class MoveAction : TurnAction
{
    private readonly Cell _originCell;
    private readonly Cell _targetCell;

    public MoveAction(Cell originCell, Cell targetCell, int apCost) : base(1, apCost)
    {
        _originCell = originCell;
        _targetCell = targetCell;
    }

    public Cell TargetCell => _targetCell;
    public Cell OriginCell => _originCell;

    public override IEnumerator Execute(Unit unit)
    {
        Debug.Log($"{unit.gameObject.name} movement started");
        unit.CurrentAction++;
        yield return unit.MoveTo(_targetCell);

        AdvanceTick();
    }
}