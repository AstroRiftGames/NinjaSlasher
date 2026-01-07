using UnityEngine;

public class NinjaGrabState<NinjaStates> : State<NinjaStates>
{
    private Controller _controller;

    public NinjaGrabState()
    {
    }

    public override void Enter()
    {
        int value = 0;
        
        _controller.View.Animator.SetInteger("GrabType", value);

    }

    public override void Execute()
    {

    }

    public override void Sleep()
    {
        _controller.ForceExitSurface();
        _controller.View.Animator.SetInteger("GrabType", 0);
    }
}
