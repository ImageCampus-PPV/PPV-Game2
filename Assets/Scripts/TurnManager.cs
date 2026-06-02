using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class TurnManager : IService
{
    public bool IsPersistance => false;

    EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();

    public void Tick()
    {
        bool playerReady = true;

        if (!IsEndOfTurn())
            return;

        //Change for each for single call
        foreach (Player player in EntityRegistry.Players)
        {
            playerReady = player.IsTurnReady;
            if (!playerReady)
                break;
        }

        if (playerReady)
        {
            //Change for each for single call
            foreach (Player player in EntityRegistry.Players)
                player.HandleMovement();

            EnemiesTurn();
        }
    }

    //This should be a controller.
    private bool IsEndOfTurn()
    {
        foreach (Unit unit in EntityRegistry.Units)
        {
            if (unit.IsMoving)
                return false;
        }

        return true;
    }

    public void EnemiesTurn()
    {
        foreach (Enemy enemy in EntityRegistry.Enemies)
            foreach (Player player in EntityRegistry.Players)
                enemy.TakeTurn(player.CurrentCell);
    }
}