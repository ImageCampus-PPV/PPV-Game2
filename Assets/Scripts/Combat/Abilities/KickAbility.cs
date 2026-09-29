using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class KickAbility : IAbility
{
    public string Name => "Kick";
    public int APCost => 1;
    public int Range => 1;
    public int Cooldown => 1;

    private int _remainingCooldown;
    public int RemainingCooldown => _remainingCooldown;
    public AnimationStates AnimationState => AnimationStates.CounterAbilty;

    private APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private KickSystem KickSystem => ServiceProvider.Instance.GetService<KickSystem>();


    public bool CanExecute(Player player, Cell targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("[Ability] No se puede ejecutar: targetCell es null.");
            return false;
        }

        if (targetCell.stander is not Enemy)
        {
            Debug.LogWarning($"[Ability] No se puede ejecutar: la celda objetivo no tiene un Enemy. " + $"Stander: {targetCell.stander}");
            return false;
        }

        if (_remainingCooldown > 0)
        {
            Debug.LogWarning($"[Ability] No se puede ejecutar: habilidad en cooldown. " + $"Cooldown restante: {_remainingCooldown}");
            return false;
        }

        if (!player.IsInAttackRange(player.CurrentCell, targetCell, Range))
        {
            Debug.LogWarning($"[Ability] No se puede ejecutar: target fuera de rango. " + $"Player: {player.CurrentCell.Coordinates}, " + $"Target: {targetCell.Coordinates}, " + $"Range: {Range}");
            return false;
        }

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