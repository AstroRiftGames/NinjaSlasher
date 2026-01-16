using UnityEngine;

public class NinjaGrabState<NinjaStates> : State<NinjaStates>
{
    private NewController _controller;

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
        //_controller.ForceExitSurface(); //TODO REVISAR
        _controller.View.Animator.SetInteger("GrabType", 0);
    }
}
