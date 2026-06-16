using ImageCampus.ToolBox.Services;
using System.Collections.Generic;

public class AbilitySystem : IService
{
    public bool IsPersistance => false;

    private readonly List<IAbility> _abilities = new();
    public IReadOnlyList<IAbility> Abilities => _abilities;

    public void RegisterAbility(IAbility ability)
    {
        if (!_abilities.Contains(ability))
            _abilities.Add(ability);
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
}