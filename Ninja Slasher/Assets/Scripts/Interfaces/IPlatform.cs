using UnityEngine;

public interface IPlatform
{
    void OnPlayerEnter(GameObject player);
    void OnPlayerExit(GameObject player);
    void OnPlatformUpdate();
}
