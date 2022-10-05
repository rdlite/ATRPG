using System;

public class UpdateStateMachine : StateMachine
{
    public virtual void UpdateState()
    {
        if (_activeState is IUpdateState updateState)
            updateState.Update();
    }

    public virtual void FixedUpdateState()
    {
        if (_activeState is IFixedUpdateState fixedUpdateState)
            fixedUpdateState.FixedUpdate();
    }

    public virtual void LateUpdateState()
    {
        if (_activeState is ILateUpdateState lateUpdateState)
            lateUpdateState.LateUpdate();
    }

    public IExitableState GetStateOfType(Type type)
    {
        foreach (var state in _states)
        {
            if (state.Key == type)
            {
                return state.Value;
            }
        }

        return null;
    }

    public IExitableState GetActiveState()
    {
        return _activeState;
    }
}