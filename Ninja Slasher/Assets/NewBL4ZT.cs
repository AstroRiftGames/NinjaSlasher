using UnityEditor.EditorTools;
using UnityEngine;

public class NewBL4ZT : MonoBehaviour
{
    #region VARIABLES
    [Header("Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundOffset = 0.15f;
    [SerializeField] private float surfaceOffset = 0.5f;
    [SerializeField] private LayerMask surfaceMask;

    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float wallCheckDistance = 0.2f;
    [SerializeField] private bool _goingRight = true;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float pivotDistance = 0.5f;

    private float turnSign;
    private bool isTurning;
    private float targetAngle;
    private Vector2 pivotPoint;

    private Vector2 currentNormal = Vector2.up;
    #endregion

    #region SURFACE DETECTION
    private bool DetectGround(float offset, out RaycastHit2D hit)
    {
        Vector2 origin =
            (Vector2)transform.position +
            (Vector2)GetMovementDir() * offset -
            (Vector2)transform.up/2;

        Vector2 direction = -transform.up;

        hit = Physics2D.Raycast(origin, direction, groundCheckDistance, surfaceMask);

        return hit.collider != null;
    }

    private bool DetectWall(out RaycastHit2D hit)
    {
        Vector2 origin = transform.position;
        Vector2 direction = GetMovementDir();

        hit = Physics2D.Raycast(origin, direction, wallCheckDistance, surfaceMask);

        return hit.collider != null;
    }

    #endregion

    #region MOVEMENT & ALIGNMENT
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
        transform.position += GetMovementDir() * speed * Time.deltaTime;
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
    #endregion

    #region TURNING
    private void StartTurn(Vector2 newNormal, bool isClosedCorner)
    {
        isTurning = true;
        currentNormal = newNormal;

        turnSign = _goingRight ? (isClosedCorner ? 1f : -1f) : (isClosedCorner ? -1f : 1f);

        Vector2 tangent = _goingRight
            ? new Vector2(newNormal.y, -newNormal.x)
            : new Vector2(-newNormal.y, newNormal.x);

        targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

        pivotPoint = (Vector2)transform.position - (Vector2)transform.up * pivotDistance;
    }

    private void UpdateTurn()
    {
        float current = transform.eulerAngles.z;

        float diff = Mathf.DeltaAngle(current, targetAngle);

        float step = rotationSpeed * Time.deltaTime * turnSign;

        if (Mathf.Sign(diff) != Mathf.Sign(step) || Mathf.Abs(step) >= Mathf.Abs(diff))
        {
            step = diff;
            isTurning = false;
        }

        transform.RotateAround(pivotPoint, Vector3.forward, step);

        transform.position += GetMovementDir() * speed * Time.deltaTime;

        if (!isTurning)
        {
            SnapToSurface();
        }
    }
    #endregion

    #region UTILS
    public void ChangeMovementDir() => _goingRight = !_goingRight;
    private Vector3 GetMovementDir() => _goingRight ? transform.right : -transform.right;
    #endregion

    #region MAGIC METHODS
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

        if (!groundFront && !groundBack)
            return;

        if (groundFront)
            AlignToSurface(frontHit.normal);
        else
            AlignToSurface(backHit.normal);

        // CLOSE CORNER
        if (wallAhead && groundFront && groundBack)
        {
            Vector2 newNormal =
                new Vector2(-currentNormal.y, currentNormal.x);

            StartTurn(newNormal, true);
            return;
        }

        // OPEN CORNER
        if (!wallAhead && groundBack && !groundFront)
        {
            Vector2 newNormal =
                new Vector2(currentNormal.y, -currentNormal.x);

            StartTurn(newNormal, false);
            return;
        }

        //MOVEMENT
        MoveAlongSurface();
        SnapToSurface();
    }
    #endregion
}