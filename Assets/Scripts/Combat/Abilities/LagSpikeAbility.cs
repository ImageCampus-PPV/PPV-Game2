using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;

public class LagSpikeAbility : IAbility
{
    public string Name => "LagSpike";
    public int APCost => 1;

    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private TurnManager TurnManager => ServiceProvider.Instance.GetService<TurnManager>();

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();

    public bool CanExecute(Player player, Cell targetCell)
    {
        if (targetCell == null)
            return false;

        if (targetCell.stander is not Enemy)
            return false;

        if (APWallet.CurrentAP < APCost)
            return false;

        return true;
    }

    public void Execute(Player player, Cell targetCell)
    {
        Enemy enemy = targetCell.stander as Enemy;

        EventBus.Raise<APConsumeRequestAceptedEvent>(APCost);

        TurnManager.ApplyStun(enemy);

        EventBus.Raise<APWalletChangeEvent>(APWallet.CurrentAP, APWallet.MaxAP);
    }
}