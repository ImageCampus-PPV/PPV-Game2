public sealed class ExplodeState : State
{
    public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
    {
        BehaviourActions behaviourActions = new BehaviourActions();
        return behaviourActions;
    }

    public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
    {
        return default;
    }

    public override BehaviourActions GetOnTickBehaviour(params object[] parameters)
    {
        BehaviourActions behaviourActions = new BehaviourActions();
        return behaviourActions;
    }
}

