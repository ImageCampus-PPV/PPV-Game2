using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

        unit.FaceCell(_targetCell);
        yield return unit.PlayAnimationAndWait(_ability.AnimationState);

        if (!_ability.CanExecute(_player, _targetCell))
        {
            unit.CustomAnimator.Play(AnimationStates.Idle);
            yield break;
        }

        _ability.Execute(_player, _targetCell);

        AdvanceTick();
        unit.CustomAnimator.Play(AnimationStates.Idle);
    }



}
