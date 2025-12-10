using UnityEngine;

public class BreakableProp : MonoBehaviour
{
    [SerializeField] BoxCollider2D _col;
    [SerializeField] GameObject _whole;
    [SerializeField] GameObject _broken;
    [SerializeField] SFXClip[] _clips;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _whole.SetActive(false);
        _col.enabled = false;
        _broken.SetActive(true);
        if (_clips.Length > 0)
        {
            AudioManager.Instance.PlaySFXAtPosition(_clips[Random.Range(0, _clips.Length)], transform.position);
        }
    }
}
