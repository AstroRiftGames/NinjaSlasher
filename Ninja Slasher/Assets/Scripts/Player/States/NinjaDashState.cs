using UnityEngine;

public class NinjaDashState<NinjaStates> : State<NinjaStates> where NinjaStates : System.Enum
{
    private Controller _controller;
    private float _dashDuration;
    private float _dashTimer;
    private TrailRenderer _trailRenderer;
    private ParticleSystem _takeoffParticles;

    public NinjaDashState()
    {
    }

    public override void Enter()
    {
        _controller.Dash();
        _dashDuration = _controller.Model.DashDuration;
        _dashTimer = _dashDuration;

        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = true;
        }
        if (_takeoffParticles != null)
        {
            _takeoffParticles.Play();
        }
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

    public override void Sleep()
    {
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }
    }
}
