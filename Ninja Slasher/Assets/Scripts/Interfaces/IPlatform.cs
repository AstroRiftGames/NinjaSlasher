using UnityEngine;

public interface IPlatform
{
    void OnPlayerEnter(GameObject player);
    void OnPlayerExit(GameObject player, bool isForced = false);
    void OnPlatformUpdate();
}
