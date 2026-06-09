public class LightEnemy : Enemy
{
    private bool _chargedAttack = false;
    public bool IsChargedAttack => _chargedAttack;

    public void ChargeAttack()
    {
        _chargedAttack = true;
    }

    public void UnchargeAttack()
    {
        _chargedAttack = false;
    }
}
