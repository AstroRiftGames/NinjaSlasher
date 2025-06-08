using UnityEngine;

public class SpikePlatform : PlatformBase
{
    [SerializeField] private float activationDelay = 1.5f;

    private float timer = -1f;
    private GameObject currentPlayer;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isActive || !collision.gameObject.CompareTag("Player")) return;

        if (currentPlayer == null)
        {
            currentPlayer = collision.gameObject;
            timer = activationDelay;
        }
    }

    public override void OnPlayerExit(GameObject player)
    {
        if (player == currentPlayer)
        {
            currentPlayer = null;
            timer = -1f;
        }
    }

    protected override void OnPlatformUpdate()
    {
        if (!isActive || currentPlayer == null) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            ActivateTrap(currentPlayer);
            currentPlayer = null;
            timer = -1f;
        }
    }

    private void ActivateTrap(GameObject player)
    {
        Debug.Log("Pinchos activados!");

        var controller = player.GetComponent<Controller>();
        if (controller != null)
        {
            controller.Die();
        }

        isActive = false;
    }

    public override void OnPlayerEnter(GameObject player) { }
}
