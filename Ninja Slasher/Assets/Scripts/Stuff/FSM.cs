using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSM<T>
{
    IState<T> _current;
    public IState<T> CurrentState => _current;
    public FSM(IState<T> initState)
    {
        SetInitalState(initState);
    }
    public void SetInitalState(IState<T> initState)
    {
        _current = initState;
        _current.SetFSM = this;
        _current.Enter();
    }
    public void OnUpdate()
    {
        if (_current != null)
            _current.Execute();
    }

    public void Transition(T input)
    {
        IState<T> newState = _current.GetTransition(input);
        if (newState != null)
        {
            _current.Sleep();
            _current = newState;
            _current.SetFSM = this;
            _current.Enter();
        }
    }
}
