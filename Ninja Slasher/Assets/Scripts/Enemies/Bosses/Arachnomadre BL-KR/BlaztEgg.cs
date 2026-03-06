using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BlaztEgg : MonoBehaviour, IPoolable
{
    [SerializeField] GameObject BlaztPrefab;
    [SerializeField] GameObject _regularEgg;
    [SerializeField] GameObject _brokenEgg;

    private Arachnomadre _arachnomadre;
    public event Action<BlaztEgg> OnRequestDespawn;

    bool _hatched = false;
    [SerializeField] float _despawnTime = 2.5f;

    public void SetArachnomadre(Arachnomadre boss) => _arachnomadre = boss;

    public void OnSpawn()
    {
        _hatched = false;
        transform.parent = null;
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_hatched && collision.gameObject.CompareTag("Floor"))
        {
            _hatched = true;
            _regularEgg.SetActive(false);
            _brokenEgg.SetActive(true);

            BL4ZT newEnemy = Instantiate(
                BlaztPrefab,
                transform.position - Vector3.up*.5f,
                Quaternion.identity
            ).GetComponentInChildren<BL4ZT>();

            newEnemy.SetRoaming(true);

            newEnemy.SetArachnomadre(_arachnomadre);
            _arachnomadre.IncreaseEggsAmount();

            StartCoroutine(RequestDespawn());
        }
    }

    private IEnumerator RequestDespawn()
    {
        yield return new WaitForSeconds(_despawnTime);
        OnRequestDespawn?.Invoke(this);
    }
}
