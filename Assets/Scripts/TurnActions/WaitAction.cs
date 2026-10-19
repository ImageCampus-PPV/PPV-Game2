using System.Collections;
using UnityEngine;

public class WaitAction : TurnAction
{
    public WaitAction() : base(1, 0)
    {

    }

    public override IEnumerator Execute(Unit unit)
    {
        unit.CurrentAction++;
        yield return unit.Wait();

        AdvanceTick();
    }
}