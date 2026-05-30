using UnityEngine;

public sealed class PatrolState : State
{
    private Transform _actualTarget;

    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        return default;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        Transform wayPoint1 = parameters[0] as Transform;
        Transform wayPoint2 = parameters[1] as Transform;
        Transform agentTransform = parameters[2] as Transform;
        Transform targetTransform = parameters[3] as Transform;
        float speed = (float)parameters[4];
        float chaseDistance = (float)parameters[5];
        float deltaTime = (float)parameters[6];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() =>
        {
            if (_actualTarget == null)
            {
                _actualTarget = wayPoint1;
            }

            if (Vector3.Distance(agentTransform.position, _actualTarget.position) < 0.1f)
            {
                if (_actualTarget == wayPoint1)
                {
                    _actualTarget = wayPoint2;
                }
                else
                {
                    _actualTarget = wayPoint1;
                }
            }

            agentTransform.position += (_actualTarget.position - agentTransform.position).normalized * speed * deltaTime;
        });

        behaviourActions.SetTransitionBehaviour(() =>
        {
            if (Vector3.Distance(agentTransform.position, targetTransform.position) <= chaseDistance)
                changeState?.Invoke(typeof(ChaseState));
        });

        return behaviourActions;
    }
}

