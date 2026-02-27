using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;

public class BreakablePlatform : PlatformBase
{
    [Header("SETTINGS")]
    [SerializeField] private int maxUses;
    [SerializeField] private float falloffVelocity;

    private Rigidbody2D playerRb;
    private int remainingUses;
    [SerializeField] int _columns = 3;
    [SerializeField] int _rows = 2;



    [SerializeField] GameObject _tilemap;
    TilemapCollider2D _tilemapCol;
    List<List<int>> pieces = new List<List<int>>();
    Vector3[] _coords;
    [Header("BREAKING")]
    [SerializeField] GameObject[] _piecesPrefabs;
    [SerializeField] float _explosionForce;

    private void Awake()
    {
        _tilemap.TryGetComponent(out TilemapCollider2D renderer);
        _tilemapCol = renderer;
    }


    protected override void InitializePlatform()
    {
        remainingUses = maxUses;
    }

    public override void OnPlayerExit(GameObject player, bool isForced = false) { }

    public override void OnPlayerEnter(GameObject player)
    {
        player.TryGetComponent(out Rigidbody2D rb);
        playerRb = rb;
        if (!isActive) return;


        remainingUses--;
        Debug.Log("Playing SFX");
        //AudioManager.Instance.PlaySFXAtPosition(Clip, player.transform.position);
        AudioService.Instance.PlaySFXAtPosition(_audioSet.Interaction, transform.position);
        if (remainingUses <= 0)
        {
            Break();
        }
    }

    public override void OnPlatformUpdate() { }

    private void Break()
    {
        isActive = false;

        GameEvents.RaiseBreakablePlatformBroken();

        //AudioManager.Instance.PlaySFXAtPosition(SFXClip.Plat_Breakable, transform.position);
        AudioService.Instance.PlaySFXAtPosition(_audioSet.DestroyPlatform, transform.position);
        DeactivateWhole();
        GeneratePieces();
        StartCoroutine(DestroyNextFrame());
    }

    private void DeactivateWhole()
    {
        _tilemap.SetActive(false);
    }

    private void GeneratePieces()
    {
        CreateMatrix();
        SetPositions();
        SpawnPieces();
    }

    private void CreateMatrix()
    {
        _coords = new Vector3[_columns*_rows];
        for (int c = 0; c < _columns; c++)
        {
            pieces.Add(new List<int>());
            for (int r = 0; r < _rows; r++)
            {
                pieces[c].Add(r);
            }
        }
    }
    private void SetPositions()
    {
        Vector3 pos = Vector2.zero;
        Vector3 offset = Vector2.zero;
        int index = 0;
        for (int c = 0; c < pieces.Count; c++)
        {
            for (int r = 0; r < pieces[c].Count; r++)
            {
                pos.x = c;
                pos.y = r;
                offset.x = c / 2;
                offset.y = r / 2;
                _coords[index] = transform.position + pos - offset;
                index++;
            }
        }
    }

    private void SpawnPieces()
    {
        foreach (var coord in _coords)
        {
            GameObject randomPiece = _piecesPrefabs[Random.Range(0, _piecesPrefabs.Length)];
            GameObject newPiece = Instantiate(randomPiece, coord, Quaternion.identity);
            newPiece.TryGetComponent(out Rigidbody2D rb);
            rb.AddForce((newPiece.transform.position - transform.position).normalized * _explosionForce, ForceMode2D.Impulse);
        }
    }
    private IEnumerator DestroyNextFrame()
    {
        yield return null;
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -falloffVelocity);
        Destroy(gameObject);
    }
}
