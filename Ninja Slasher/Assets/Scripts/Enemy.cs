using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] EnemyData _data;

    protected virtual void OnEnable()
    {

    }
    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }


    public void Die()
    {
        Debug.Log($"{_data.Type} killed");
        Destroy(gameObject);
    }
}
