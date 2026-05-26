using System;

public abstract class State
{
    public Action<Type> changeState;
    public abstract BehaviourActions GetOnEnterBehaviour(params object[] parameters);
    public abstract BehaviourActions GetOnTickBehaviour(params object[] parameters);
    public abstract BehaviourActions GetOnExitBehaviour(params object[] parameters);
}