using System.Collections;
using UnityEngine;

public class AbilityAction : TurnAction, IAttackAction
{
    private Player _player;
    private IAbility _ability;
    private Cell _targetCell;

    public AbilityAction(Player player, IAbility ability, Cell targetCell, int totalTicks, int APCost) : base(totalTicks, APCost)
    {
        _ability = ability;
        _targetCell = targetCell;
        _player = player;
    }

    public override IEnumerator Execute(Unit unit)
    {
        unit.CurrentAction++;
        if (_ability.CanExecute(_player, _targetCell))
            _ability.Execute(_player, _targetCell);

        yield return null;
        AdvanceTick();
    }
}
