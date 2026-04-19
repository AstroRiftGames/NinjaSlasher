using UnityEngine;

public class BreakableProp : MonoBehaviour
{
    [SerializeField] BoxCollider2D _col;
    [SerializeField] GameObject _whole;
    [SerializeField] GameObject _broken;
    [SerializeField] AudioEvent[] _audioEvents;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Break();
    }

    public void Break()
    {
        _whole.SetActive(false);
        _col.enabled = false;
        _broken.SetActive(true);

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
