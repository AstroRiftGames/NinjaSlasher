using UnityEngine;

public class NinjaParryState<NinjaStates> : State<NinjaStates>
{
    private Controller _controller;
    private float _currentParryTimer;

    public NinjaParryState(Controller controller)
    {
        _controller = controller;
    }

    public override void Enter()
    {
        Debug.Log("Tried Parry");
        Collider2D[] hits = Physics2D.OverlapCircleAll(_controller.transform.position, 2f, LayerMask.GetMask("Projectiles"));

        _controller.StartParry(hits);
        _currentParryTimer = _controller.GetParryWindow();
    }

    public override void Execute()
    {
        _currentParryTimer -= Time.deltaTime;
        if (_currentParryTimer <= 0)
        {
            _controller.SetIsParrying(false);
        }
    }

    public override void Sleep()
    {
        _controller.SetIsParrying(false);
    }
}
