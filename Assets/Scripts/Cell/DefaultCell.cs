using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public abstract class CellState : State
{
    protected MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
}

[CellState(0.50f, 0.50f, 0.50f, 1)]
public sealed class DefaultCell : CellState
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.AddCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }
}

[CellState(0, 0, 0, 1)]
public sealed class Broken : CellState
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];
        Renderer renderer = (Renderer)parameters[1];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.AddCell(cell); renderer.material.color = Color.black; });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell);});

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }
}

[CellState(1, 1, 1, 1f)]
public sealed class Empty : CellState
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];
        Renderer renderer = (Renderer)parameters[1];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.AddCell(cell); renderer.material.color = Color.black; });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }
}

[CellState(1, 0.6f, 0f, 1)]
public sealed class Unstable : CellState
{
    private int _maxTurnsAlive = 0;
    private int _turnsSinceCreated = 0;
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];
        Renderer renderer = (Renderer)parameters[1];
        int turnToBeDestroy = (int)parameters[2];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
          () => { MapGrid.AddCell(cell); this._maxTurnsAlive = turnToBeDestroy; renderer.material.color = Color.gray; }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
            (
            () =>
            {
                if (_maxTurnsAlive == ++_turnsSinceCreated)
                    changeState.Invoke(typeof(Broken));
            }
            );

        return behaviourActions;
    }
}

[CellState(0, 1f, 0f, 1.0f)]
public sealed class Healing : CellState
{
    private uint _healing = 20;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];
        Renderer renderer = (Renderer)parameters[1];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
            () =>
            {
                MapGrid.AddCell(cell);
                renderer.material.color = Color.yellow;
            }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        Unit unitOnTop = (Unit)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
            () =>
            {
                if (unitOnTop is Player player)
                    player.AddHp(_healing);
            }
        );

        return behaviourActions;
    }
}

[CellState(0.62f, 0.125f, 0.94f, 1.0f)]
public class Infected : CellState
{
    protected uint _damage = 0;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];
        uint damage = (uint)parameters[1];
        Renderer renderer = (Renderer)parameters[2];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { this._damage = damage; renderer.material.color = Color.purple; });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        Unit unitOnTop = (Unit)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
         (
            () =>
            {
                if (unitOnTop is Player player)
                    player.ReduceHp(_damage);
            }
        );

        return behaviourActions;
    }
}

[CellState(1.0f, 0.078f, 0.5764f, 1.0f)]
public class Contagious : Infected
{
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    private Vector2Int _position;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];
        uint damage = (uint)parameters[1];
        Vector2Int position = (Vector2Int)parameters[2];
        Renderer renderer = (Renderer)parameters[3];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
            () =>
            {
                MapGrid.AddCell(cell);
                renderer.material.color = Color.deepPink;
                _damage = damage;
                _position = position;
            }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        Cell cell = (Cell)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { MapGrid.RemoveCell(cell); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        Unit unitOnTop = (Unit)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
         (
            () =>
            {
                if (unitOnTop is Player player)
                    player.ReduceHp(_damage);

                changeState.Invoke(typeof(Infected));
            }
        );

        return behaviourActions;
    }
}