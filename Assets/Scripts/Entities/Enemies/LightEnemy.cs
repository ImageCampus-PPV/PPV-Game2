public class LightEnemy : Enemy
{
    LightEnemy()
    {
        _damage = 10;
        _attackRange = 1;
        _movementRange = 1;
        _fortitude = 2;
        _pushDistance = 3;
        _maxTicksPerTurn = base.MaxTicksPerTurn;
        _attackTickCost = 1;
    }

    protected override void PlanCombatActions(Cell playerCell)
    {
        AttackAction attackAction = new AttackAction(this, playerCell.stander, Damage, _attackTickCost);
        if (CanAddAction(attackAction))
            _plannedActions.Add(attackAction);
    }
}
