using System;
using System.Collections.Generic;

public class FSM
{
    private Type _currentState;
    private Dictionary<Type, State> _states;
    private Dictionary<Type, Func<object[]>> _behaviourOnTickParameters;
    private Dictionary<Type, Func<object[]>> _behaviourOnEnterParameters;
    private Dictionary<Type, Func<object[]>> _behaviourOnExitParameters;

    private BehaviourActions GetCurrentTickBehaviour => _states[_currentState].GetOnTickBehaviour
        (_behaviourOnTickParameters[_currentState]?.Invoke());
    private BehaviourActions GetCurrentOnEnterBehaviour => _states[_currentState].GetOnEnterBehaviour
        (_behaviourOnEnterParameters[_currentState]?.Invoke());
    private BehaviourActions GetCurrentOnExitBehaviour => _states[_currentState].GetOnExitBehaviour
        (_behaviourOnExitParameters[_currentState]?.Invoke());

    public FSM(Type defaultState)
    {
        _states = new Dictionary<Type, State>();

        _behaviourOnTickParameters = new Dictionary<Type, Func<object[]>>();
        _behaviourOnEnterParameters = new Dictionary<Type, Func<object[]>>();
        _behaviourOnExitParameters = new Dictionary<Type, Func<object[]>>();
        ForceState(defaultState);
    }

    public void AddState<TState>(
        Func<object[]> onTickParameters = null,
        Func<object[]> onEnterParameters = null,
        Func<object[]> onExitParameters = null)
        where TState : State, new()
    {
        Type stateType = typeof(TState);

        if (_states.ContainsKey(stateType))
            return;

        TState state = new TState();
        state.changeState += Transition;
        _states.Add(stateType, state);
        _behaviourOnTickParameters.Add(stateType, onTickParameters);
        _behaviourOnEnterParameters.Add(stateType, onEnterParameters);
        _behaviourOnExitParameters.Add(stateType, onExitParameters);

    }

    private void ForceState(Type state)
    {
        _currentState = state.GetType();
    }

    public void Transition(Type stateType)
    {
        if (_states.ContainsKey(_currentState))
            ExcecuteBehaviour(GetCurrentOnExitBehaviour);

        _currentState = stateType;
        ExcecuteBehaviour(GetCurrentOnEnterBehaviour);
    }

    public void Tick()
    {
        if (!_states.ContainsKey(_currentState))
            return;

        ExcecuteBehaviour(GetCurrentTickBehaviour);
    }

    private void ExcecuteBehaviour(BehaviourActions behaviourActions)
    {
        if (behaviourActions.Equals(default))
            return;

        behaviourActions.MainThreadBehaviours?.Invoke();

        behaviourActions.TransitionBehaviour?.Invoke();
    }
}