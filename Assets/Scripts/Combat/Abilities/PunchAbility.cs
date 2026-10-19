using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class PunchAbility : IAbility
{
    public string Name => "Punch";
    public int APCost => 1;
    public int Range => 2;
    public int Cooldown => 2;

    private int _remainingCooldown;
    public int RemainingCooldown => _remainingCooldown;
    public AnimationStates AnimationState => AnimationStates.LagSpikeAbility;

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private TurnManager TurnManager => ServiceProvider.Instance.GetService<TurnManager>();

    public bool CanExecute(Player player, Cell targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("[Ability] CanExecute failed: targetCell is null.");
            return false;
        }

        if (targetCell.stander is not Enemy)
        {
            Debug.LogWarning($"[Ability] CanExecute failed: target cell {targetCell.Coordinates} " + $"does not contain an Enemy. Stander: {targetCell.stander}"); return false;
        }

        if (_remainingCooldown > 0)
        {
            Debug.LogWarning($"[Ability] CanExecute failed: ability is on cooldown. " + $"Remaining cooldown: {_remainingCooldown}");
            return false;
        }

        if (!player.IsInAttackRange(player.CurrentCell, targetCell, Range))
        {
            Debug.LogWarning($"[Ability] CanExecute failed: target is out of range. " + $"Player cell: {player.CurrentCell?.Coordinates}, " + $"Target cell: {targetCell.Coordinates}, " + $"Range: {Range}");
            return false;
        }

        Debug.Log($"[Ability] CanExecute succeeded. " + $"Target: {targetCell.Coordinates}, " + $"AP: {APWallet.CurrentAP}/{APCost}, " + $"Range: {Range}, " + $"Cooldown: {_remainingCooldown}");

        return true;
    }

    public void Execute(Player player, Cell targetCell)
    {
        Enemy enemy = targetCell.stander as Enemy;

        //EventBus.Raise<APConsumeRequestAceptedEvent>(APCost);

        TurnManager.ApplyStun(enemy);
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