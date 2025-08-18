using System.Runtime.CompilerServices;
using UnityEngine;

public class BlaztEgg : MonoBehaviour
{
    [SerializeField] GameObject BlaztPrefab;
    private Arachnomadre _arachnomadre;
    bool _hatched = false;
    public void SetArachnomadre(Arachnomadre boss) => _arachnomadre = boss;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(!_hatched && collision.gameObject.CompareTag("Floor"))
        {
            _hatched = true;
            BL4ZT newEnemy = Instantiate(BlaztPrefab, transform.position + Vector3.up *.5f, Quaternion.identity).GetComponentInChildren<BL4ZT>();
            newEnemy.SetArachnomadre(_arachnomadre);
            _arachnomadre.IncreaseEggsAmount();
            Destroy(gameObject, .5f);
        }
    }
}
