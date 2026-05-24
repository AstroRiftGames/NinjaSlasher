using System.Collections;
using UnityEngine;

public enum MagneticState
{
    Off,
    Attract,
    Repel
}

public class MagneticPlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private float radius;
    [SerializeField] private float force;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private ParticleSystem attractionParticles;
    [SerializeField] private ParticleSystem reppelingParticles;
    [SerializeField] private MagneticState _currentState = MagneticState.Attract;

    public MagneticState CurrentState => _currentState;

    protected override void InitializePlatform()
    {
        base.InitializePlatform();

        // Inicializar el estado físico y visual en Start sin disparar audio o triggers de animación
        SetState(_currentState, playEffects: false);
    }

    public void SetState(MagneticState newState)
    {
        SetState(newState, true);
    }

    public void SetState(MagneticState newState, bool playEffects)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RecordObject(this, "Set Magnetic Platform State");
        }
#endif

        _currentState = newState;

        // Ajustar el signo de la fuerza según el comportamiento deseado.
        // Fuerza positiva atrae, fuerza negativa repele.
        if (_currentState == MagneticState.Attract)
        {
            force = Mathf.Abs(force);
        }
        else if (_currentState == MagneticState.Repel)
        {
            force = -Mathf.Abs(force);
        }

        // Transiciones del Animator
        if (playEffects && _animator != null && _animator.isActiveAndEnabled)
        {
            switch (_currentState)
            {
                case MagneticState.Attract:
                    _animator.SetTrigger("SetAttract");
                    break;
                case MagneticState.Repel:
                    _animator.SetTrigger("SetRepel");
                    break;
                case MagneticState.Off:
                    _animator.SetTrigger("SetOff");
                    break;
            }
        }

        // Efectos de sonido (sólo en runtime para evitar errores en editor)
        if (playEffects && Application.isPlaying)
        {
            if (AudioService.Instance != null && _audioContext != null && _audioContext.Audio != null)
            {
                AudioService.Instance.PlaySFXAtPosition(_audioContext.Audio.Interaction, transform.position);
            }
        }

        // Control visual de Sistemas de Partículas
        if (_currentState == MagneticState.Attract)
        {
            if (reppelingParticles != null) reppelingParticles.Stop();
            if (attractionParticles != null) attractionParticles.Play();
        }
        else if (_currentState == MagneticState.Repel)
        {
            if (attractionParticles != null) attractionParticles.Stop();
            if (reppelingParticles != null) reppelingParticles.Play();
        }
        else // MagneticState.Off
        {
            if (attractionParticles != null) attractionParticles.Stop();
            if (reppelingParticles != null) reppelingParticles.Stop();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    public override void OnPlatformUpdate()
    {
        if (_currentState == MagneticState.Off) return;

        if (Application.isPlaying && AudioService.Instance != null && _audioContext != null && _audioContext.Audio != null)
        {
            AudioService.Instance.PlaySFXAtPosition(
                _currentState == MagneticState.Attract ? _audioContext.Audio.Idle : _audioContext.Audio.Idle2,
                transform.position
            );
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, playerLayer);

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
            rb.AddForce(direction * force, ForceMode2D.Force);
        }
    }

    public override void OnPlayerEnter(GameObject player) { }

    public override void OnPlayerExit(GameObject player, bool isForced = false)
    {
        StartCoroutine(ReleasePlayer());
    }

    private IEnumerator ReleasePlayer()
    {
        MagneticState previousState = _currentState;

        // Si estaba atrayendo al salir, transiciona temporalmente a repeler para liberar al jugador
        if (previousState == MagneticState.Attract)
        {
            SetState(MagneticState.Repel);
            yield return new WaitForSeconds(.75f);

            // Revertir a Attract únicamente si el estado no cambió en el transcurso
            if (_currentState == MagneticState.Repel)
            {
                SetState(MagneticState.Attract);
            }
        }
        else
        {
            yield return null;
        }
    }
}
