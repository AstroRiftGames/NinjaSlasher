using UnityEngine;

public class NinjaIdleState<NinjaStates> : State<NinjaStates>
{
    NewController _controller;

    public NinjaIdleState()
    {
    }

    public override void Enter()
    {
    }

    public override void Execute()
    {
    }

    public override void Sleep()
    {
        _controller.View.Animator.SetInteger("GrabType", 0);
    }
}
