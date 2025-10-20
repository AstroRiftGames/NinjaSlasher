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
        PlayKOSFX();
    }

    private void PlayKOSFX()
    {
        int r = Random.Range(1, 8);
        SFXClip clip = r switch
        {
            1 => SFXClip.P_KO_1,
            2 => SFXClip.P_KO_2,
            3 => SFXClip.P_KO_3,
            4 => SFXClip.P_KO_4,
            5 => SFXClip.P_KO_5,
            6 => SFXClip.P_KO_6,
            7 => SFXClip.P_KO_7,
            _ => SFXClip.P_KO_1
        };

        AudioManager.Instance.PlaySFXAtPosition(clip, _controller.transform.position);
    }
}
