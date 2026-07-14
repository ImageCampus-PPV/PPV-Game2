public class HeavyEnemy : Enemy
{
    protected override void PlanCombatActions(Cell playerCell)
    {
        if (!IsCellNearUnit(playerCell, AttackRange))
            return;
        //_plannedActions.Add(new AttackAction(this, playerCell.stander, Damage, 1, 0));
    }
}