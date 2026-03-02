using UnityEngine;

public class GeyserDeathCol : MonoBehaviour
{
    [SerializeField] private Transform _anchor;
    [SerializeField] private Transform _target;
    private BoxCollider2D _col;


    private void Awake()
    {
        TryGetComponent(out BoxCollider2D col);
        _col = col;
    }

    private void OnEnable()
    {
        CustomUpdateManager.Instance.SubscribeToFixedUpdate(CustomUpdate);
    }

    private void OnDisable()
    {
        CustomUpdateManager.Instance.UnsubscribeFromFixedUpdate(CustomUpdate);
    }

    private void CustomUpdate()
    {
        Vector2 dif = Vector2.zero;
        dif.x = Mathf.Abs(_anchor.position.x - _target.position.x);
        dif.y = Mathf.Abs(_anchor.position.y - _target.position.y);

        AdjustSize(dif);
        AdjustOffset(dif);
    }

    private void AdjustSize(Vector2 dif)
    {
        Vector2 newSize = _col.size;
        newSize.y = dif.y;
        _col.size = newSize;
    }

    private void AdjustOffset(Vector2 dif)
    {
        _col.offset = dif / 2;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out NewController player))
        {
            player.Die();
        }
        else if (collision.TryGetComponent(out Enemy enemy))
        {
            enemy.Die();
        }
    }
}
