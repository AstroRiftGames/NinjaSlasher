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

        Debug.Log("Entering Idle State");
    }

    public override void Execute()
    {
        
    }

    public override void Sleep()
    {
        _controller.View.Animator.SetBool("IsGrounded", false);

        Debug.Log("Exiting Idle State");
    }
}
