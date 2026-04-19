using UnityEngine;

public class InteractableProp : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] AudioEvent[] _audioEvents;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _anim.SetTrigger("OnHit");

        AudioEvent audioEvent = GetRandomAudioEvent();
        if (audioEvent != null)
        {
            AudioService.Instance?.PlaySFXAtPosition(audioEvent, transform.position);
        }
    }

    private AudioEvent GetRandomAudioEvent()
    {
        if (_audioEvents == null || _audioEvents.Length == 0)
        {
            return null;
        }

        return _audioEvents[Random.Range(0, _audioEvents.Length)];
    }
}
