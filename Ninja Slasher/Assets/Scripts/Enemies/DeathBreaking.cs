using UnityEngine;

public class DeathBreaking : MonoBehaviour
{
    [SerializeField] Enemy _enemy;

    public void BreakEnemy()
    {
        if (_enemy != null)
        {
            _enemy.StartBreaking();
        }
    }
}
