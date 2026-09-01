using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;

public class KickAbility : IAbility
{
    public string Name => "Kick";
    public int APCost => 1;
    public int Range => 1;
    public int Cooldown => 1;

    private int _remainingCooldown;
    public int RemainingCooldown => _remainingCooldown;

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private KickSystem KickSystem => ServiceProvider.Instance.GetService<KickSystem>();

    public bool CanExecute(Player player, Cell targetCell)
    {
        if (targetCell == null)
            return false;

        if (targetCell.stander is not Enemy)
            return false;

        if (APWallet.CurrentAP < APCost)
            return false;

        if (_remainingCooldown > 0)
            return false;

        if (!player.IsInAttackRange(player.CurrentCell, targetCell, Range))
            return false;
        //if (!TurnManager.IsCellNearUnit(player.CurrentCell, targetCell, Range))
        //    return false;

        return true;
    }

    public void Execute(Player player, Cell targetCell)
    {
        Enemy enemy = targetCell.stander as Enemy;

        //EventBus.Raise<APConsumeRequestAceptedEvent>(APCost);
        KickSystem.Execute(player, enemy);
        StartCooldown();
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }

    public void StartCooldown()
    {
        _remainingCooldown = Cooldown;
        EventBus.Raise<AbilityCooldownChangedEvent>(this, _remainingCooldown);
    }

    public void TickCooldown()
    {
        if (_remainingCooldown > 0)
            _remainingCooldown--;

        EventBus.Raise<AbilityCooldownChangedEvent>(this, _remainingCooldown);
    }
}