using UnityEngine;

public class SlipperyPlatform : PlatformBase
{
    [Header("Horizontal Slide")]
    [SerializeField] private float slideSpeed = 4f;
    [SerializeField] private float falloffVelocity;

    [Header("Vertical Slide (Gravity Simulation)")]
    [Tooltip("Velocidad máxima al caer por la plataforma vertical")]
    [SerializeField] private float maxFallSpeed = 6f;
    [Tooltip("Aceleración gravitacional aplicada mientras desliza")]
    [SerializeField] private float slideGravity = 8f;
    [Tooltip("Velocidad inicial hacia arriba cuando el impacto tiene componente ascendente")]
    [SerializeField] private float upwardImpulse = 3f;

    private Rigidbody2D playerRb;
    private PlayerController playerController;

    // Slide horizontal
    private Vector2 slideDirection;

    // Slide vertical
    private float _verticalVelocity;   // velocidad actual en el eje vertical del deslizamiento
    private bool _isVertical;          // ¿la plataforma es una pared?

    private bool _isSliding = false;

    public override void OnPlayerEnter(GameObject player)
    {
        playerController = player.GetComponent<PlayerController>();
        if (playerController == null) return;

        PlayerView view = playerController.View;
        if (view == null) return;

        playerRb = view.RB;
        if (playerRb == null) return;

        // Determinar si la plataforma es vertical (normal apunta horizontalmente)
        _isVertical = Mathf.Abs(transform.up.x) > 0.9f;

        if (_isVertical)
        {
            // El player impacta una pared: calcular si venía con componente hacia arriba
            Vector2 incomingDir = playerController.LastMoveDirection;
            float upwardComponent = incomingDir.y; // positivo = venía moviéndose hacia arriba

            // Si golpeó con impulso hacia arriba, le damos una velocidad inicial positiva
            // que después la gravedad irá frenando y revirtiendo.
            _verticalVelocity = upwardComponent > 0f ? upwardImpulse : 0f;
        }
        else
        {
            // Plataforma horizontal: comportamiento original por tangente
            Vector2 tangent = new Vector2(transform.up.y, -transform.up.x);
            Vector2 incomingDir = playerController.LastMoveDirection;
            float sign = Mathf.Sign(Vector2.Dot(incomingDir, tangent));
            slideDirection = tangent * sign;
        }

        playerRb.linearVelocity = Vector2.zero;

        if (!_isVertical)
            playerController.SetLastMoveDirection(slideDirection);

        _isSliding = true;

        AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, player.transform.position);
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false)
    {
        if (playerRb == null) return;

        if (isForced)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
        else
        {
            bool isHorizontal = transform.up.y > 0.9f;
            bool isRightWall   = transform.up.x > 0.9f;

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
        _isSliding       = false;
        _verticalVelocity = 0f;
        playerRb         = null;
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

        if (_isVertical)
        {
            // Simular gravedad: acelerar hacia abajo continuamente
            _verticalVelocity -= slideGravity * Time.deltaTime;

            // Clampear para que no supere la velocidad máxima de caída
            _verticalVelocity = Mathf.Max(_verticalVelocity, -maxFallSpeed);

            playerRb.linearVelocity = new Vector2(0f, _verticalVelocity);
        }
        else
        {
            // Comportamiento original para plataformas horizontales
            playerRb.linearVelocity = slideDirection * slideSpeed;
        }
    }
}
