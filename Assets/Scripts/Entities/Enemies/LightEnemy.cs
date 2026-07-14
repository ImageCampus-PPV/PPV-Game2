public class LightEnemy : Enemy
{
    private bool _isChargedAttack = false;
    public bool IsChargedAttack => _isChargedAttack;

    public void ChargeAttack()
    {
        _isChargedAttack = true;
    }

    public void UnchargeAttack()
    {
        _isChargedAttack = false;
    }

    protected override void PlanCombatActions(Cell playerCell)
    {
        if (!IsCellNearUnit(playerCell, AttackRange))
            return;
        //_plannedActions.Add(new AttackAction(this, playerCell.stander, Damage, 1, 0));
        //TODO
    }
}
