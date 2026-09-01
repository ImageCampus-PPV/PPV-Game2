using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;

public class AbilitySystem : IService
{

    public bool IsPersistance => false;

    private readonly Dictionary<Type, IAbility> _abilities = new();

    public void Init()
    {
        EventBus eventBus = ServiceProvider.Instance.GetService<EventBus>();
        eventBus.Subscribe<OnTurnEndEvent>(OnTurnStart);

        RegisterAbility(new PunchAbility());
        RegisterAbility(new KickAbility());
    }

    public void RegisterAbility(IAbility ability)
    {
        if (ability == null)
            return;

        _abilities[ability.GetType()] = ability;
    }

    public T GetAbility<T>() where T : class, IAbility
    {
        if (_abilities.TryGetValue(typeof(T), out IAbility ability))
            return ability as T;

        return null;
    }

    public bool UseAbility<T>(Player player, Cell targetCell) where T : class, IAbility
    {
        T ability = GetAbility<T>();

        if (ability == null)
            return false;

        if (!ability.CanExecute(player, targetCell))
            return false;

        ability.Execute(player, targetCell);
        return true;
    }

    public bool UseAbility(IAbility ability, Player player, Cell targetCell)
    {
        if (ability == null)
            return false;

        if (!ability.CanExecute(player, targetCell))
            return false;

        ability.Execute(player, targetCell);
        return true;
    }

    public void TickCooldowns()
    {
        foreach (IAbility ability in _abilities.Values)
            ability.TickCooldown();
    }

    private void OnTurnStart(in OnTurnEndEvent callback)
    {
        TickCooldowns();
    }
}