using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using UnityEngine;

public sealed class DefaultCell : State
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
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

public sealed class Unstable : State
{
    private int _turnToBeDestroy = 0;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        int turnToBeDestroy = (int)parameters[0];
        Renderer renderer = (Renderer)parameters[1];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
        (
          () => { this._turnToBeDestroy = turnToBeDestroy; renderer.material.color = Color.gray; }
        );

        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default(BehaviourActions);
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        int currentTurn = (int)parameters[0];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour
            (
            () =>
            {
                if (_turnToBeDestroy == currentTurn)
                    changeState.Invoke(typeof(Broken));
            }
            );

        return behaviourActions;
    }
}

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