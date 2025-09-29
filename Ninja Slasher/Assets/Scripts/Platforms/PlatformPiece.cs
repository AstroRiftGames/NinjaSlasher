using System.Collections;
using UnityEngine;

public class PlatformPiece : MonoBehaviour
{
    SpriteRenderer _renderer;

    private void Awake()
    {
        TryGetComponent(out SpriteRenderer renderer);
        _renderer = renderer;
    }

    private void Start()
    {
        StartCoroutine(Despawn());
    }

    private IEnumerator Despawn()
    {
        float a = 1;
        while (a > 0)
        {
            _renderer.color = new Color(_renderer.color.r, _renderer.color.g, _renderer.color.b, _renderer.color.a - .001f);
            a = _renderer.color.a;
            yield return null;
        }
        Destroy(gameObject);
    }
}
