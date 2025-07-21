using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class NinjaIdleState<NinjaStates> : State<NinjaStates>
{
    Controller _controller;

    // Swipe
    private Vector2 swipeStart;
    private Vector2 endTouchPosition;
    private Vector2 currentSwipe;
    private bool _isSwiping = false;
    private float minSwipeDistance;

    public NinjaIdleState(Controller controller) 
    {
        _controller = controller;
    }

    public override void Enter()
    {
        _controller.View.RB.linearVelocity = Vector2.zero;
        _controller.View.Animator.SetBool("IsGrounded", true);
    }

    public override void Execute()
    {

    }

    public override void Sleep()
    {
        _controller.View.Animator.SetBool("IsGrounded", true);
    }
}
