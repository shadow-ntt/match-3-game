using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Quan ly sinh item moi tu tren cao (Spawn From Sky) khi ben duoi thieu item
public class SpawnItemFromSky : Singleton<SpawnItemFromSky>
{
    [SerializeField] private Board board;
    [SerializeField] private float fallDuration = 0.2f;
    [SerializeField] private Ease fallEase = Ease.InQuad;
    [SerializeField] private float spawnSpacing = 1.0f;
    [SerializeField] private List<EnumItemBoard> allowedColors = new List<EnumItemBoard>
    {
        EnumItemBoard.Red,
        EnumItemBoard.Blue,
        EnumItemBoard.Green,
        EnumItemBoard.Yellow
    };

    private bool _isSpawning;

    public Board Board => board;
    public bool IsSpawning => _isSpawning;
    public List<EnumItemBoard> AllowedColors => allowedColors;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null) board = FindAnyObjectByType<Board>();
    }

    // Wrapper bat dong bo cho viec sinh item tu tren cao
    public async UniTask<bool> SpawnFromSkyAsync()
    {
        if (_isSpawning)
        {
            return false;
        }

        var utcs = new UniTaskCompletionSource<bool>();
        bool triggered = SpawnFromSky(() => utcs.TrySetResult(true));
        if (!triggered) return false;

        return await utcs.Task;
    }

    // Quet cac o trong tren ban co va sinh ngoc moi tu tren cao roi xuong
    public bool SpawnFromSky(Action onComplete = null)
    {
        if (_isSpawning)
        {
            onComplete?.Invoke();
            return false;
        }

        int width = board.Width;
        int height = board.Height;
        int spawnCount = 0;
        int activeTweens = 0;

        Transform parent = board.NormalTilemap != null ? board.NormalTilemap.transform : board.transform;

        for (int x = 0; x < width; x++)
        {
            List<int> emptyYList = GetEmptyYList(x);
            if (emptyYList.Count == 0) continue;

            int topY = GetTopY(x);
            if (topY < 0) continue;

            // 2. Vi tri goc tren troi cua cot x (ngay phia tren o cao nhat cua cot)
            Vector3 spawnOrigin = GridUtils.GridToWorld(board.Grid, x, topY + 1);

            // 3. Sinh va tao animation roi cho tung o trong
            for (int i = 0; i < emptyYList.Count; i++)
            {
                int targetY = emptyYList[i];
                GameObject cellObj = board.BoardCellGrid[x, targetY];
                BoardCell targetCell = cellObj != null ? cellObj.GetComponent<BoardCell>() : null;

                // Chon ngau nhien 1 mau ngoc tu danh sach mau cho phep
                int colorIndex = UnityEngine.Random.Range(0, allowedColors.Count);
                int colorId = (int)allowedColors[colorIndex];

                // Tinh toa do xuat phat tren troi (xep hang dan len tren)
                Vector3 startPos = spawnOrigin + Vector3.up * (spawnSpacing * i);
                GameObject newObj = Pooltem.Instance.SpawnBoardItem(colorId, startPos, Quaternion.identity, parent);
                if (newObj == null) continue;

                newObj.name = $"NormalItem_{colorId}_{x}_{targetY}";

                // Cap nhat trang thai va vi tri NormalGrid ngay lap tuc
                board.NormalGrid[x, targetY] = newObj;
                if (targetCell != null)
                {
                    targetCell.State = EnumStateBoardCell.Falling;
                }

                // Tinh thoi gian va khoang cach roi
                Vector3 targetWorldPos = GridUtils.GridToWorld(board.Grid, x, targetY);
                float distance = (topY + 1 + i) - targetY;
                float duration = fallDuration * Mathf.Sqrt(distance);

                spawnCount++;
                activeTweens++;

                newObj.transform.DOMove(targetWorldPos, duration).SetEase(fallEase).OnComplete(() =>
                {
                    if (targetCell != null)
                    {
                        targetCell.State = EnumStateBoardCell.Occupied;
                    }

                    activeTweens--;
                    if (activeTweens == 0)
                    {
                        _isSpawning = false;
                        onComplete?.Invoke();
                    }
                });
            }
        }

        if (spawnCount > 0)
        {
            _isSpawning = true;
        }
        else
        {
            onComplete?.Invoke();
        }

        return spawnCount > 0;
    }

    // Lay danh sach cac toa do Y dang trong cua cot x (xep tu duoi len tren)
    public List<int> GetEmptyYList(int x)
    {
        List<int> emptyYList = new List<int>();
        int height = board.Height;

        for (int y = 0; y < height; y++)
        {
            GameObject cellObj = board.BoardCellGrid[x, y];
            if (cellObj == null) continue;

            if (cellObj.TryGetComponent<BoardCell>(out var cell) && cell.IsEmpty && board.NormalGrid[x, y] == null)
            {
                emptyYList.Add(y);
            }
        }

        return emptyYList;
    }

    // Lay toa do Y cao nhat co BoardCell hop le cua cot x
    public int GetTopY(int x)
    {
        for (int y = board.Height - 1; y >= 0; y--)
        {
            if (board.BoardCellGrid[x, y] != null) return y;
        }
        return -1;
    }
}
