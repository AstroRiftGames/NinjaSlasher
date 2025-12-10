using UnityEngine;

public class InteractableProp : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] SFXClip[] _clips;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _anim.SetTrigger("OnHit");
        if(_clips.Length > 0)
        {
            AudioManager.Instance.PlaySFXAtPosition(_clips[Random.Range(0, _clips.Length)], transform.position);
        }
    }
}
