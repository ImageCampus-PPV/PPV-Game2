public class HeavyEnemy : Enemy
{
    HeavyEnemy()
    {
        _damage = 40;
        _attackRange = 1;
        _movementRange = 1;
        _fortitude = 7;
        _pushDistance = 1;
        _maxTicksPerTurn = 5;
        _attackTickCost = 1;
    }

    protected override void PlanCombatActions(Cell playerCell)
    {
        WaitAction chargeAction = new WaitAction();
        if (CanAddAction(chargeAction))
            _plannedActions.Add(chargeAction);
        else
            return;

        AttackAction attackAction = new AttackAction(this, playerCell.stander, Damage, _attackTickCost);
        if (CanAddAction(attackAction))
            _plannedActions.Add(attackAction);
    }
}