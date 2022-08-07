using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachine : IStateMachine {
    protected Dictionary<Type, IExitableState> _states;
    protected IExitableState _activeState;

    public void Enter<TState>() where TState : class, IState {
        ChangeState<TState>().Enter();
    }

    public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadState<TPayload> {
        ChangeState<TState>().Enter(payload);
    }

    private TState ChangeState<TState>() where TState : class, IExitableState {
        _activeState?.Exit();

        TState state = GetState<TState>();
        _activeState = state;

        return state;
    }

    private TState GetState<TState>() where TState : class, IExitableState {
        return _states[typeof(TState)] as TState;
    }
}