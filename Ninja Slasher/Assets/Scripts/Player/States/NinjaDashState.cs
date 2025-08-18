using UnityEngine;

public class NinjaDashState<NinjaStates> : State<NinjaStates> where NinjaStates : System.Enum
{
    private Controller _controller;
    private float _dashDuration;
    private float _dashTimer;

    public NinjaDashState(Controller controller)
    {
        _controller = controller;
    }

    public override void Enter()
    {
        _controller.Dash();
        _dashDuration = _controller.Model.DashDuration;
        _dashTimer = _dashDuration;
    }

    public override void Execute()
    {
        _dashTimer -= Time.deltaTime;

        if (_dashTimer <= 0)
        {
            if (_controller.HasCurrentSurface())
            {
                _fsm.Transition((NinjaStates)(object) Controller.NinjaStates.Grab);
            }
            else
            {
                _fsm.Transition((NinjaStates)(object) Controller.NinjaStates.Idle);
            }
        }
    }
}
