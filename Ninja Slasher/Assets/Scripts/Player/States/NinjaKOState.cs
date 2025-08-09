using UnityEngine;

public class NinjaKOState<NinjaStates> : State<NinjaStates>
{
    private Controller _controller;

    public NinjaKOState(Controller controller)
    {
        _controller = controller;
    }

    public override void Enter()
    {
        _controller.Die();
        _controller.View.Animator.SetTrigger("OnKO");
    }

    public override void Execute()
    {

    }

    public override void Sleep()
    {
    }
}
