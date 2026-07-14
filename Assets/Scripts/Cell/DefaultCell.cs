using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

[CellState(0.50f, 0.50f, 0.50f, 1)]
public sealed class DefaultCell : State
{
    // Antes esto no hacia nada, asi que una Cell que volvia a DefaultCell
    // (ej. via Purificacion de una Terminal, MEC-02) se quedaba visualmente
    // con el color de su estado anterior (Infected/Contagious/etc) aunque
    // logicamente ya no estuviera corrupta. Se agrega el reset de color para
    // que coincida con el resto de los estados (Broken, Healing, etc), que si
    // aplican su color en GetOnEnterBehaviour.
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        if (parameters == null || parameters.Length == 0 || parameters[0] is not Renderer renderer)
            return default(BehaviourActions);

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 1f); });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }
}

[CellState(0, 0, 0, 1)]
public sealed class Broken : State
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Renderer renderer = (Renderer)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { renderer.material.color = Color.black; });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }
}

[CellState(1, 1, 1, 1f)]
public sealed class Empty : State
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Renderer renderer = (Renderer)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { renderer.material.color = Color.black; });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }
}

[CellState(1, 0.6f, 0f, 1)]
public sealed class Unstable : State
{
    private int _maxTurnsAlive = 0;
    private int _turnsSinceCreated = 0;
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Renderer renderer = (Renderer)parameters[0];
        int turnToBeDestroy = (int)parameters[1];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
          () => { this._maxTurnsAlive = turnToBeDestroy; renderer.material.color = Color.gray; }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
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
public sealed class Healing : State
{
    private uint _healing = 20;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        Renderer renderer = (Renderer)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
            () =>
            {
                renderer.material.color = Color.yellow;
            }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
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
                    player.AddLife(_healing);
            }
        );

        return behaviourActions;
    }
}

[CellState(0.62f, 0.125f, 0.94f, 1.0f)]
public class Infected : State
{
    protected uint _damage = 0;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        uint damage = (uint)parameters[0];
        Renderer renderer = (Renderer)parameters[1];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() => { this._damage = damage; renderer.material.color = Color.purple; });

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
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
                    player.ReduceLife(_damage);
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
        uint damage = (uint)parameters[0];
        Vector2Int position = (Vector2Int)parameters[1];
        Renderer renderer = (Renderer)parameters[2];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
            () =>
            {
                renderer.material.color = Color.deepPink;
                _damage = damage;
                _position = position;
            }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        Unit unitOnTop = (Unit)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
         (
            () =>
            {
                EventBus.Raise<InfectTilesEvent>(_position);

                if (unitOnTop is Player player)
                    player.ReduceLife(_damage);

                changeState.Invoke(typeof(Infected));
            }
        );

        return behaviourActions;
    }
}