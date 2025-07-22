using UnityEngine;

public class SpikePlatform : PlatformBase
{
    [Header("Spike Settings")]
    [SerializeField] private float activationDelay = 2f;
    [SerializeField] private GameObject spikeObject;

    private float timer = -1f;
    private bool isCounting = false;
    private GameObject currentPlayer;

    protected override void InitializePlatform()
    {
        if (spikeObject != null)
            spikeObject.SetActive(false);
    }

    public override void OnPlayerEnter(GameObject player)
    {
        if (!isActive || isCounting) return;

        Debug.Log("[SPIKE] Jugador entró en la plataforma");

        currentPlayer = player;
        timer = activationDelay;
        isCounting = true;
    }

    public override void OnPlayerExit(GameObject player)
    {
        if (player == currentPlayer && isCounting)
        {
            isCounting = false;
            timer = -1f;
            currentPlayer = null;
        }
    }

    public override void OnPlatformUpdate()
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
        if (spikeObject != null)
        {
            spikeObject.SetActive(true);
        }

        if (currentPlayer != null)
        {
            Controller controller = currentPlayer.GetComponent<Controller>();
            if (controller != null)
            {
                controller.Die();
            }
        }

        isCounting = false;
        timer = -1f;
        currentPlayer = null;
        isActive = false;
    }

    public void ResetPlatform()
    {
        isActive = true;
        isCounting = false;
        timer = -1f;
        currentPlayer = null;

        if (spikeObject != null)
            spikeObject.SetActive(false);
    }
}