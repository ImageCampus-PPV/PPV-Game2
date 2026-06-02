using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public class TurnManager : IService
{
    public bool IsPersistance => false;

    EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();
    APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();

    public void Tick()
    {
        //The input shouldn't be handle here
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsEndOfTurn())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Cells")))
            {
                if (hit.collider.TryGetComponent<Cell>(out Cell clickedCell))
                    foreach (Player player in EntityRegistry.Players)
                        player.HandleMovement(clickedCell);

            }

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