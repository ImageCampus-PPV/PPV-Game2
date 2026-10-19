using UnityEngine;

public class LightEnemy : Enemy
{
    LightEnemy()
    {
        _damage = 10;
        _attackRange = 1;
        _movementRange = 1;
        _fortitude = 2;
        _pushDistance = 3;
        _maxTicksPerTurn = base.MaxTicksPerTurn - 1;
        _attackTickCost = 1;
    }

    protected override void PlanCombatActions(Cell playerCell)
    {
        AttackAction attackAction = new AttackAction(this, playerCell.stander, Damage, _attackTickCost);
        if (CanAddAction(attackAction))
            _plannedActions.Add(attackAction);
    }

    public override void FaceCell(Cell targetCell)
    {
        if (targetCell == null || CurrentCell == null)
            return;

        Vector2Int direction = targetCell.Coordinates - CurrentCell.Coordinates;

        if (direction.x > 0)
            FaceLeft();
        else if (direction.x < 0)
            FaceRight();
        else if (direction.y > 0)
            FaceRight();
        else if (direction.y < 0)
            FaceLeft();
    }
}
