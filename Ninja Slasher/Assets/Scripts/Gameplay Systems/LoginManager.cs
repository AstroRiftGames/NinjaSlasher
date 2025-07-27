using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using UnityEngine;

public class LoginManager : MonoBehaviour
{

    void Start()
    {
        //PlayGamesPlatform.Activate();
        Social.localUser.Authenticate(success =>
        {
            if (success)
            {
                Debug.Log("Login exitoso perri");
                // Load del progreso
            }
            else
            {
                Debug.Log("Fallo el login gato");
            }
        });
    }
}
