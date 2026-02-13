using UnityEngine;

public class NewBL4ZT : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundOffset = 0.15f;
    [SerializeField] private float surfaceOffset = 0.5f;
    [SerializeField] private LayerMask surfaceMask;

    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float wallCheckDistance = 0.2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float pivotDistance = 0.5f;

    private bool isTurning;
    private float targetAngle;
    private Vector2 pivotPoint;

    private Vector2 currentNormal = Vector2.up;

    // ----------------------------------------------------

    private bool DetectGround(float offset, out RaycastHit2D hit)
    {
        Vector2 origin =
            (Vector2)transform.position +
            (Vector2)transform.right * offset -
            (Vector2)transform.up/2;

        Vector2 direction = -transform.up;

        hit = Physics2D.Raycast(origin, direction, groundCheckDistance, surfaceMask);
        Debug.DrawRay(origin, direction * groundCheckDistance, Color.yellow);

        return hit.collider != null;
    }

    private bool DetectWall(out RaycastHit2D hit)
    {
        Vector2 origin = transform.position;
        Vector2 direction = transform.right;

        hit = Physics2D.Raycast(origin, direction, wallCheckDistance, surfaceMask);
        Debug.DrawRay(origin, direction * wallCheckDistance, Color.red);

        return hit.collider != null;
    }

    // ----------------------------------------------------

    private void AlignToSurface(Vector2 normal)
    {
        currentNormal = normal;

        Vector2 tangent = new Vector2(normal.y, -normal.x);

        if (Vector2.Dot(tangent, transform.right) < 0)
            tangent = -tangent;

        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void MoveAlongSurface()
    {
        transform.position += transform.right * speed * Time.deltaTime;
    }
    private void SnapToSurface()
    {
        Vector2 origin = transform.position;
        Vector2 direction = -transform.up;

        RaycastHit2D hit =
            Physics2D.Raycast(origin, direction, 2f, surfaceMask);

        Debug.DrawRay(origin, direction * 2f, Color.cyan);

        if (!hit.collider)
            return;

        float delta = hit.distance - surfaceOffset;

        transform.position -= (Vector3)transform.up * delta;
    }

    private void StartTurn(Vector2 newNormal)
    {
        isTurning = true;

        Vector2 tangent = new Vector2(newNormal.y, -newNormal.x);
        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

        targetAngle = angle;

        // pivote en la base del enemigo
        pivotPoint =
            (Vector2)transform.position
            - (Vector2)transform.up * pivotDistance;
    }
    private void UpdateTurn()
    {
        float current = transform.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(
            current,
            targetAngle,
            rotationSpeed * Time.deltaTime);

        float delta = next - current;

        transform.RotateAround(pivotPoint, Vector3.forward, delta);

        // avanzar mientras gira
        transform.position += transform.right * speed * Time.deltaTime;

        if (Mathf.Abs(Mathf.DeltaAngle(next, targetAngle)) < 0.5f)
        {
            isTurning = false;
            SnapToSurface();   // 🔥 CLAVE
        }
    }

    // ----------------------------------------------------

    private void Update()
    {
        if (isTurning)
        {
            UpdateTurn();
            return;
        }

        bool groundFront = DetectGround(groundOffset, out RaycastHit2D frontHit);
        bool groundBack = DetectGround(-groundOffset, out RaycastHit2D backHit);
        bool wallAhead = DetectWall(out RaycastHit2D wallHit);

        // Si no hay nada debajo, no hacemos lógica
        if (!groundFront && !groundBack)
            return;

        // Usamos la mejor normal disponible
        if (groundFront)
            AlignToSurface(frontHit.normal);
        else
            AlignToSurface(backHit.normal);

        // ------------------------------------------------
        // CASO 1 — ESQUINA CERRADA
        // ------------------------------------------------
        if (wallAhead && groundFront && groundBack)
        {
            // Rotar normal actual 90° antihorario
            Vector2 newNormal =
                new Vector2(-currentNormal.y, currentNormal.x);

            StartTurn(newNormal);
            return;
        }

        // ------------------------------------------------
        // CASO 2 — ESQUINA ABIERTA
        // ------------------------------------------------
        if (!wallAhead && groundBack && !groundFront)
        {
            Vector2 newNormal =
                new Vector2(currentNormal.y, -currentNormal.x);

            StartTurn(newNormal);
            return;
        }

        // ------------------------------------------------
        // Movimiento normal
        // ------------------------------------------------
        MoveAlongSurface();
        SnapToSurface();
    }
}