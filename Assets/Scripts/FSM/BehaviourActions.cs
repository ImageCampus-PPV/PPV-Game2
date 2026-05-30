using System;

public struct BehaviourActions
{
    private Action _updateBehaviours;
    private Action _transitionBehaviour;

    public Action UpdateBehaviours => _updateBehaviours;
    public Action TransitionBehaviour => _transitionBehaviour;

    public void AddUpdateBehaviour(Action behaviour)
    {
        _updateBehaviours = behaviour;
    }

    public void SetTransitionBehaviour(Action behaviour)
    {
        _transitionBehaviour = behaviour;
    }
}

