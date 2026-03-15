using UnityEngine;

public class ArachnomadreFurtiveAttack : MonoBehaviour
{
    [SerializeField] private Arachnomadre _arachnomadre;
    public void Attack()
    {
        _arachnomadre.Bite();
    }
}
