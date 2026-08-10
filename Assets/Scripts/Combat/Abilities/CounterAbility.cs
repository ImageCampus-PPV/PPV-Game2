using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;

public class CounterAbility : IAbility
{
    public string Name => "Counter";
    public int APCost => 1;
    public int Range => 1;

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private CounterSystem CounterSystem => ServiceProvider.Instance.GetService<CounterSystem>();

    public bool CanExecute(Player player, Cell targetCell)
    {
        if (targetCell == null)
            return false;

        if (targetCell.stander is not Enemy)
            return false;

        if (APWallet.CurrentAP < APCost)
            return false;

        //if (!TurnManager.IsCellNearUnit(player.CurrentCell, targetCell, Range))
        //    return false;

        return true;
    }

    public void Execute(Player player, Cell targetCell)
    {
        Enemy enemy = targetCell.stander as Enemy;

        //EventBus.Raise<APConsumeRequestAceptedEvent>(APCost);
        CounterSystem.Execute(player, enemy);
        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }
}