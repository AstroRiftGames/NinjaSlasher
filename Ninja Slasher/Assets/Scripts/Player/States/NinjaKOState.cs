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

        Debug.Log("Entering KO State");
    }

    public override void Execute()
    {

    }

    public override void Sleep()
    {
        Debug.Log("Exiting KO State");
    }
}
