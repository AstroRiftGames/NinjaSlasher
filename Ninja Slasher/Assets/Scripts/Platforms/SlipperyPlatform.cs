using System.Collections.Generic;
using UnityEngine;

public class SlipperyPlatform : PlatformBase
{
    [SerializeField] private float slideSpeed = 4f;
    [SerializeField] private float falloffVelocity;

    private Rigidbody2D playerRb;
    private NewController playerController;
    private Vector2 slideDirection;
    private bool _isSliding = false;

    public override void OnPlayerEnter(GameObject player)
    {
        playerController = player.GetComponent<NewController>();
        if (playerController == null) return;

        View view = playerController.View;
        if (view == null) return;

        playerRb = view.RB;
        if (playerRb == null) return;

        Vector2 tangent = new Vector2(transform.up.y, -transform.up.x);
        Vector2 incomingDir = playerController.LastDashDirection;
        float sign = Mathf.Sign(Vector2.Dot(incomingDir, tangent));
        slideDirection = tangent * sign;

        playerRb.linearVelocity = Vector2.zero;
        _isSliding = true;

        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, player.transform.position);
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false)
    {
        if (playerRb == null)
        {
            return;
        }   

        if (isForced)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
        else
        {
            bool isHorizontal = transform.up.y > 0.9f;
            bool isRightWall = transform.up.x > 0.9f;

            if (isHorizontal)
            {
                playerRb.linearVelocityY = -falloffVelocity;
            }
            else
            {
                playerRb.linearVelocityX = -falloffVelocity * (isRightWall ? 1 : -1);
            }
        }

        ResetValues();
    }

    private void ResetValues()
    {
        _isSliding = false;
        playerRb = null;
        playerController = null;
    }

    public override void OnPlatformUpdate()
    {
        if (!_isSliding || playerRb == null || playerController == null) return;

        if (playerController.IsDashing)
        {
            _isSliding = false;
            return;
        }

        playerRb.linearVelocity = slideDirection * slideSpeed;
    }
}
