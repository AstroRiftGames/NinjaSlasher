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
        _controller.View.Animator.SetBool("IsGrounded", true);
    }

    public override void Execute()
    {
        
    }

    public override void Sleep()
    {
        _controller.View.Animator.SetBool("IsGrounded", false);
    }
}
