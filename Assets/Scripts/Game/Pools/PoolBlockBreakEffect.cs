using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

[Serializable]
public class ShatteredItemData
{
    [Tooltip("Loai item tren ban co (Red = 102, Blue = 103, Green = 104, Yellow = 105, ...)")]
    public EnumItemBoard itemId;

    [Tooltip("Cac sprite manh vo cua item nay")]
    public Sprite[] sprites;
}

public class PoolBlockBreakEffect : Singleton<PoolBlockBreakEffect>
{
    [Header("Cau hinh Manh Vo Cho Tung Item")]
    [SerializeField] private ShatteredItemData[] itemDataList;

    [Header("Prefab & Pool")]
    [Tooltip("Prefab ShatteredPiece (neu de trong, script se tu dong khoi tao runtime)")]
    [SerializeField] private ShatteredPiece piecePrefab;
    [SerializeField] private int initialPoolSize = 30;

    [Header("Cau hinh Animation")]
    [Tooltip("Khoang cach / toc do vang manh")]
    [SerializeField] private float explosionSpeed = 1.1f;
    [Tooltip("Thoi gian manh vo ton tai truoc khi bien mat")]
    [SerializeField] private float duration = 0.45f;
    [Tooltip("Kich thuoc manh vo")]
    [SerializeField] private float pieceScale = 1.0f;
    [Tooltip("Sorting Order de manh vo hien thi tren cac o ban co")]
    [SerializeField] private int sortingOrder = 25;

    // Cache tra cuu nhanh sprite theo EnumItemBoard
    private readonly Dictionary<EnumItemBoard, Sprite[]> _itemSpriteMap = new Dictionary<EnumItemBoard, Sprite[]>();

    // Hang doi pool cac manh vo dung chung
    private readonly Queue<ShatteredPiece> _pool = new Queue<ShatteredPiece>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        Init();
    }

    private void Init()
    {
        _itemSpriteMap.Clear();
        if (itemDataList != null)
        {
            foreach (var data in itemDataList)
            {
                if (data != null && data.sprites != null && data.sprites.Length > 0)
                {
                    _itemSpriteMap[data.itemId] = data.sprites;
                }
            }
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            _pool.Enqueue(InstantiatePiece());
        }
    }

    private ShatteredPiece InstantiatePiece()
    {
        ShatteredPiece piece;
        if (piecePrefab != null)
        {
            piece = Instantiate(piecePrefab, transform);
        }
        else
        {
            GameObject go = new GameObject("ShatteredPiece_Runtime");
            go.transform.SetParent(transform);
            go.AddComponent<SpriteRenderer>();
            piece = go.AddComponent<ShatteredPiece>();
        }

        piece.gameObject.SetActive(false);
        return piece;
    }

    private ShatteredPiece GetPiece()
    {
        while (_pool.Count > 0)
        {
            ShatteredPiece piece = _pool.Dequeue();
            if (piece != null) return piece;
        }

        return InstantiatePiece();
    }

    private void ReturnPieceToPool(ShatteredPiece piece)
    {
        if (piece != null)
        {
            _pool.Enqueue(piece);
        }
    }

    // Phat hieu ung theo Transform goc va EnumItemBoard
    public void Play(Transform origin, EnumItemBoard itemId)
    {
        if (origin != null)
        {
            Play(origin.position, itemId);
        }
    }

    // Phat hieu ung theo toa do Vector3 va EnumItemBoard
    public void Play(Vector3 position, EnumItemBoard itemId)
    {
        // Truong hop fallback hoac item chua co sprite cau hinh: khong lam gi ca (silent no-op)
        if (!_itemSpriteMap.TryGetValue(itemId, out var sprites) || sprites == null || sprites.Length == 0)
        {
            return;
        }

        int count = sprites.Length;
        float baseAngleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null) continue;

            ShatteredPiece piece = GetPiece();
            if (piece == null) continue;

            // Tinh huong bay phan bo deu 360 do voi do lech ngau nhien
            float angle = (i * baseAngleStep) + UnityEngine.Random.Range(-baseAngleStep * 0.35f, baseAngleStep * 0.35f);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            float speed = explosionSpeed * UnityEngine.Random.Range(0.85f, 1.25f);

            piece.SetSorting(sortingOrder);
            piece.Play(position, sprite, dir, speed, duration, pieceScale, ReturnPieceToPool);
        }
    }

    // Overload ho tro EnumItemColor neu co noi goi den
    public void Play(Vector3 position, EnumItemColor color)
    {
        EnumItemBoard itemId = (EnumItemBoard)((int)color + 101);
        Play(position, itemId);
    }

    public void Play(Transform origin, EnumItemColor color)
    {
        if (origin != null)
        {
            Play(origin.position, color);
        }
    }

    // Overload ho tro int id
    public void Play(Vector3 position, int id)
    {
        Play(position, (EnumItemBoard)id);
    }

    public void Play(Transform origin, int id)
    {
        if (origin != null)
        {
            Play(origin.position, (EnumItemBoard)id);
        }
    }
}
