using UnityEngine;

public class NinjaDashState<NinjaStates> : State<NinjaStates> where NinjaStates : System.Enum
{
    private Controller _controller;
    private float _dashDuration; // How long the dash should last
    private float _dashTimer;    // Timer for the dash

    public NinjaDashState(Controller controller)
    {
        _controller = controller;
    }

    public override void Enter()
    {
        // Logic to execute when entering the Dash state.
        _controller.Dash(); // This triggers the force application
        _dashDuration = _controller.Model.DashDuration; // Assuming you have a DashDuration in your Model
        _dashTimer = _dashDuration; // Initialize timer
        Debug.Log("Entering Dash State. Dash will last for: " + _dashDuration + " seconds.");
    }

    public override void Execute()
    {
        // Logic to execute every frame while in the Dash state.
        _dashTimer -= Time.deltaTime;

        if (_dashTimer <= 0)
        {
            // Dash duration ended, transition back to Idle or Grab (if on surface)
            // The FSM.Transition will handle calling Sleep() on this state.
            if (_controller.HasCurrentSurface())
            {
                _fsm.Transition((NinjaStates)(object) Controller.NinjaStates.Grab); // Explicit cast
            }
            else
            {
                _fsm.Transition((NinjaStates)(object) Controller.NinjaStates.Idle); // Explicit cast
            }
        }
    }

    public override void Sleep()
    {
        // Logic to execute when exiting the Dash state.
        _controller.SetIsDashing(false);
        _controller.View.Animator.SetBool("IsGrounded", false); // Assuming IsGrounded is tied to dashing
        // Optionally, zero out remaining velocity if the dash ends abruptly
        // _controller.View.RB.linearVelocity = Vector2.zero;
        Debug.Log("Exiting Dash State.");
    }
}
