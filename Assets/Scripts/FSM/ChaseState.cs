using UnityEngine;

public sealed class ChaseState : State
{
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
        Transform agentTrnasform = parameters[0] as Transform;
        Transform targetTransform = parameters[1] as Transform;
        float speed = (float)parameters[2];
        float explodeDistance = (float)parameters[3];
        float lostDistance = (float)parameters[4];
        float deltaTime = (float)parameters[5];

        BehaviourActions behaviourActions = new BehaviourActions();

        behaviourActions.AddUpdateBehaviour(() =>
        {
            agentTrnasform.position += (targetTransform.position - agentTrnasform.position).normalized * speed * deltaTime;
        });

        behaviourActions.SetTransitionBehaviour(() =>
        {
            if (Vector3.Distance(agentTrnasform.position, targetTransform.position) < explodeDistance)
                changeState.Invoke(typeof(ExplodeState));

            if (Vector3.Distance(agentTrnasform.position, targetTransform.position) > lostDistance)
                changeState.Invoke(typeof(PatrolState));
        });

        return behaviourActions;
    }
}

