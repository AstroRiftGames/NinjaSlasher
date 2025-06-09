using UnityEngine;

public class SpikePlatform : PlatformBase
{
    [Header("Spike Settings")]
    [SerializeField] private float activationDelay;
    [SerializeField] private GameObject spikeObject;

    [SerializeField] private Animator animator;

    private float timer = -1f;
    private bool isCounting = false;
    private GameObject currentPlayer;

    protected override void InitializePlatform()
    {
        if (spikeObject != null)
            spikeObject.SetActive(false);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isActive || isCounting || !collision.gameObject.CompareTag("Player")) return;

        currentPlayer = collision.gameObject;
        timer = activationDelay;
        isCounting = true;

        var view = currentPlayer.GetComponent<View>();
        if (view != null && view.RB != null)
            view.RB.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetBool("IsShaking", true);
    }

    public override void OnPlayerExit(GameObject player)
    {

    }

    protected override void OnPlatformUpdate()
    {
        if (!isActive || !isCounting) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            ActivateTrap();
        }
    }

    private void ActivateTrap()
    {
        if (animator != null)
            animator.SetBool("IsShaking", false);

        Debug.Log("Pinchos activados!");

        if (spikeObject != null)
            spikeObject.SetActive(true);

        isCounting = false;
        timer = -1f;
        currentPlayer = null;
        isActive = false;
    }

    public override void OnPlayerEnter(GameObject player) { }
}