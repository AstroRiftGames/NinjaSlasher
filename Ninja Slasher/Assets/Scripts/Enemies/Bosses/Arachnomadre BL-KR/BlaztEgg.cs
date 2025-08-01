using UnityEngine;

public class BlaztEgg : MonoBehaviour
{
    [SerializeField] GameObject BlaztPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Scenario"))
        {
            Instantiate(BlaztPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject, .5f);
        }
    }
}
