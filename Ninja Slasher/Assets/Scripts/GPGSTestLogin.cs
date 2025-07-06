using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;

public class GPGSTestLogin : MonoBehaviour
{
    private void Start()
    {
        // Activar la plataforma al inicio (si no lo hiciste ya)
        PlayGamesPlatform.Activate();
    }

    private void OnGUI()
    {
        // Botón de LOGOUT
        if (GUI.Button(new Rect(20, 20, 200, 60), "Cerrar sesión GPGS"))
        {
            ((PlayGamesPlatform)Social.Active).SignOut();
            Debug.Log("Sesión cerrada en GPGS");
        }

        // Botón de LOGIN
        if (GUI.Button(new Rect(20, 100, 200, 60), "Iniciar sesión GPGS"))
        {
            Social.localUser.Authenticate(success =>
            {
                Debug.Log(success ? "Login exitoso en GPGS" : "Login fallido");
            });
        }
    }
}
