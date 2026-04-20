using System.Collections;
using UnityEngine;

public class MagneticPlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private float attractionRadius;
    [SerializeField] private float attractionForce;
    [SerializeField] private LayerMask playerLayer;
    private bool _isAttracting = true;

    public void SwitchAttraction()
    {
        _isAttracting = !_isAttracting;
        attractionForce = -attractionForce;
        _animator.SetTrigger("OnSwtich");
        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, transform.position);
    }
    public override void OnPlatformUpdate()
    {
        AudioService.Instance.PlaySFXAtPosition(_isAttracting ? _audioContext.Audio.Idle : _audioContext.Audio.Idle2, transform.position);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attractionRadius, playerLayer);

        foreach (var hit in hits)
        {
            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController == null || !playerController.IsDashing || playerController.IsParrying)
                continue;

            PlayerView playerView = hit.GetComponent<PlayerView>();
            if (playerView == null) continue;

            Rigidbody2D rb = playerView.RB;
            if (rb == null) continue;

            Vector2 direction = ((Vector2)transform.position - rb.position).normalized;
            rb.AddForce(direction * attractionForce, ForceMode2D.Force);
        }
    }

    public override void OnPlayerEnter(GameObject player) { }
    public override void OnPlayerExit(GameObject player, bool isForced = false) 
    {
        StartCoroutine(ReleasePlayer());
    }

    private IEnumerator ReleasePlayer()
    {
        _isAttracting = false;
        yield return new WaitForSeconds(.75f);
        _isAttracting = true;
        
    }
}
