using UnityEngine;

public class NinjaIdleState<NinjaStates> : State<NinjaStates>
{
    Controller _controller;

    public NinjaIdleState(Controller controller)
    {
        _controller = controller;
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
