using UnityEngine;

public class NinjaGrabState<NinjaStates> : State<NinjaStates>
{
    private Controller _controller;

    public NinjaGrabState(Controller controller)
    {
        _controller = controller;
    }

    public override void Enter()
    {
        _controller.View.Animator.SetBool("IsGrounded", true);

        //TODO: Change Animator's "GrabType" parameter depending on what surface (wall, ceiling or floor) the player collided with.
        //TODO: If collided with wall, set Animator's "IsMirrored" parameter depending on which wall (right or left).


        _controller.View.Animator.SetBool("IsGrounded", false);
        Debug.Log("Entering Grab State");
    }

    public override void Execute()
    {

    }

    public override void Sleep()
    {
        _controller.ForceExitSurface();
        _controller.View.Animator.SetInteger("GrabType", 0);
        
        Debug.Log("Exiting Grab State");
    }
}
