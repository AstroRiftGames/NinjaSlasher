using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] EnemyData _data;

    private EnemyTracker tracker;

    protected virtual void OnEnable()
    {

    }
    protected virtual void Start()
    {
        tracker = FindObjectOfType<EnemyTracker>();
    }

    protected virtual void Update()
    {

    }


    public void Die()
    {
        Debug.Log($"{_data.Type} killed");
        tracker.OnEnemyKilled(this);
        Destroy(gameObject);
    }
}
