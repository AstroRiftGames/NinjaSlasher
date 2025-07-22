using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState<T>
{
    FSM<T> SetFSM { set; }
    public void Enter();
    public void Execute();
    public void Sleep();
    void AddTransition(T input, IState<T> state);

    IState<T> GetTransition(T input);
}
