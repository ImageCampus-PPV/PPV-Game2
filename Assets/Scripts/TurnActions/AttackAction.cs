using System.Collections;

public class AttackAction : TurnAction, IAttackAction
{
    private readonly Unit _target;
    private readonly uint _damage;

    public AttackAction(Unit attacker, Unit target, uint damage, int totalTicks, int apCost = 0) : base(totalTicks, apCost)
    {
        _target = target;
        _damage = damage;
    }

    public override IEnumerator Execute(Unit unit)
    {
        if (_target != null)
        {
            if (_target is Player player)
                player.ReduceHp(_damage);
        }

        AdvanceTick();

        yield return null;
    }
}