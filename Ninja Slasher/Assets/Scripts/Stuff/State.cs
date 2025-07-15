using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State<T> : IState<T>
{
    protected FSM<T> _fsm;
    private Dictionary<T, IState<T>> _transitions;
    public State() => _transitions = new Dictionary<T, IState<T>>();
    public State(Dictionary<T, IState<T>> transitions) => _transitions = transitions;
    public FSM<T> SetFSM { set { _fsm = value; } }
    public virtual void Enter() { }
    public virtual void Execute() { }
    public virtual void Sleep() { }
    public void AddTransition(T input, IState<T> state) => _transitions[input] = state;
    public IState<T> GetTransition(T input)
    {
        if (_transitions.ContainsKey(input))
        {
            return _transitions[input];
        }
        return null;
    }
}
