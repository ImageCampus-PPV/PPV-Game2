using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class CounterSystem : IService
{
    public bool IsPersistance => false;
    private MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    private TurnManager TurnManager => ServiceProvider.Instance.GetService<TurnManager>();

    public void Execute(Player player, Enemy enemy)
    {
        Vector2Int direction = enemy.CurrentCell.Coordinates - player.CurrentCell.Coordinates;
        direction.x = Mathf.Clamp(direction.x, -1, 1);
        direction.y = Mathf.Clamp(direction.y, -1, 1);

        PushEnemy(enemy, direction, enemy.PushDistance);
    }

    private void PushEnemy(Enemy enemy, Vector2Int direction, int distance)
    {
        Cell current = enemy.CurrentCell;

        for (int i = 0; i < distance; i++)
        {
            Vector2Int next = current.Coordinates + direction;

            if (!IsInsideMap(next))
            {
                TurnManager.ApplyStun(enemy);
                return;
            }

            Cell nextCell = MapGrid.GetCell(next);

            if (!nextCell.IsWalkable)
            {
                TurnManager.ApplyStun(enemy);
                return;
            }

            if (nextCell.stander is Enemy otherEnemy)
            {
                ResolveCollision(enemy, otherEnemy, direction, distance - i);
                return;
            }

            current = nextCell;
            enemy.MoveInstant(current);
        }

    }

    private void ResolveCollision(Enemy enemyA, Enemy enemyB, Vector2Int direction, int remainingDistance)
    {
        if (enemyA.Fortitude > enemyB.Fortitude)
        {
            TurnManager.ApplyStun(enemyB);

            PushEnemy(enemyB, direction, remainingDistance + 1);
            MoveForward(enemyA, direction, remainingDistance);
        }
        else if (enemyA.Fortitude < enemyB.Fortitude)
        {
            TurnManager.ApplyStun(enemyA);
        }
        else
        {
            PushEnemy(enemyB, direction, remainingDistance + 1);
            MoveForward(enemyA, direction, remainingDistance);
        }
    }

    private void MoveForward(Enemy enemy, Vector2Int direction, int distance)
    {
        Cell current = enemy.CurrentCell;

        for (int i = 0; i < distance; i++)
        {
            Vector2Int next = current.Coordinates + direction;

            if (!IsInsideMap(next))
                break;

            Cell nextCell = MapGrid.GetCell(next);

            if (!nextCell.IsWalkable)
                break;

            if (nextCell.stander != null)
                break;

            current = nextCell;
        }

        enemy.MoveInstant(current);
    }

    private bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 &&
               pos.y >= 0 &&
               pos.x < MapGrid.Width &&
               pos.y < MapGrid.Height;
    }
}