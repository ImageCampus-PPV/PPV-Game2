public class NormalEnemy : Enemy
{
    NormalEnemy()
    {
        _damage = 25;
        _attackRange = 1;
        _movementRange = 1;
        _fortitude = 4;
        _pushDistance = 2;
        _maxTicksPerTurn = 5;
        _attackTickCost = 1;
    }

    protected override void PlanCombatActions(Cell playerCell)
    {
        AttackAction attackAction = new AttackAction(this, playerCell.stander, Damage, _attackTickCost);
        if (CanAddAction(attackAction))
            _plannedActions.Add(attackAction);

    }
}
