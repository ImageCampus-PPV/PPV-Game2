using System;

public struct BehaviourActions
{
    private Action _mainThreadBehaviours;
    private Action _transitionBehaviour;

    public Action MainThreadBehaviours => _mainThreadBehaviours;
    public Action TransitionBehaviour => _transitionBehaviour;

    public void AddMainTrheadableBehaviour(Action behaviour)
    {
        _mainThreadBehaviours = behaviour;
    }

    public void SetTransitionBehaviour(Action behaviour)
    {
        _transitionBehaviour = behaviour;
    }
}

