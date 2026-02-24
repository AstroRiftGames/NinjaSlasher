using UnityEngine;

public class DeathSFX : MonoBehaviour
{
    [SerializeField] Enemy _enemy;

    public void PlayDeathSFX()
    {
        if (_enemy.AudioContext != null)
        {
            AudioService.Instance.PlaySFXAtPosition(_enemy.AudioContext.Audio.death, _enemy.transform.position);
        }
    }
}
