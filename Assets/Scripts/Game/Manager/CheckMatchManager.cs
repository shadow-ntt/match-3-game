using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Quan ly kiem tra match va xu ly no lien hoan (Cascade) tren ban co
public class CheckMatchManager : Singleton<CheckMatchManager>
{
    [SerializeField] private Board board;
    [SerializeField] private PoolBlockBreakEffect poolParticle;
    [SerializeField] private float explosionDelay = 0.2f;

    private LineScanMatchChecker _matchChecker;
    private bool _isProcessing;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;
    public bool IsProcessing => _isProcessing;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (board == null)
        {
            board = FindAnyObjectByType<Board>();
        }

        if (poolParticle == null)
        {
            poolParticle = FindAnyObjectByType<PoolBlockBreakEffect>();
        }
    }

    private void Start()
    {
        _matchChecker = new LineScanMatchChecker(board);
    }

    // Kiem tra nhanh co match tren ban co hay khong
    public bool HasMatches(Vector2Int? priorityCenter = null)
    {
        return _matchChecker.FindAllMatches(priorityCenter).Count > 0;
    }

    // Kiem tra match tren ban co va xu ly chuoi no lien hoan (Cascade / Combo) bang vong lap while
    public async UniTask<bool> CheckMatch(Vector2Int? priorityCenter = null)
    {
        var initialMatches = _matchChecker.FindAllMatches(priorityCenter);
        if (initialMatches.Count == 0) return false;

        _isProcessing = true;
        Vector2Int? currentCenter = priorityCenter;

        // Vong lap xu ly no lien hoan
        while (true)
        {
            var matches = _matchChecker.FindAllMatches(currentCenter);
            if (matches.Count == 0) break;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                switch (match.MatchType)
                {
                    case MatchType.Normal:
                        HandleNormalMatch(match);
                        break;
                    case MatchType.HorizontalRocket:
                        HandleNormalMatch(match);
                        break;
                    case MatchType.VerticalRocket:
                        HandleNormalMatch(match);
                        break;
                    case MatchType.TNT:
                        HandleNormalMatch(match);
                        break;
                    case MatchType.Missile:
                        HandleNormalMatch(match);
                        break;
                    case MatchType.LightBall:
                        HandleNormalMatch(match);
                        break;
                }
            }

            // Cho hieu ung hat no hien thi truoc khi item roi xuong
            if (explosionDelay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
            }

            // Vong lap dam bao ban co luon duoc lap day hoan toan (ca roi thang, roi cheo va spawn tu troi)
            while (true)
            {
                bool fell = false;
                if (ItemFallManager.Instance != null)
                {
                    fell = await ItemFallManager.Instance.OnItemFallAsync();
                }

                bool spawned = false;
                if (SpawnItemFromSky.Instance != null)
                {
                    spawned = await SpawnItemFromSky.Instance.SpawnFromSkyAsync();
                }

                if (!fell && !spawned)
                {
                    break;
                }
            }

            // Sau luot no dau tien thi cac luot no tiep theo khong con tam uu tien
            currentCenter = null;
        }

        _isProcessing = false;
        return true;
    }

    // Xu ly MatchType.Normal: phat particle no, tra item ve pool va xoa khoi grid
    private void HandleNormalMatch(MatchData match)
    {
        for (int j = 0; j < match.Matches.Count; j++)
        {
            Vector2Int pos = match.Matches[j];
            GameObject itemObj = board.NormalGrid[pos.x, pos.y];
            if (itemObj == null) continue;

            poolParticle.Play(itemObj.transform.position, (EnumItemBoard)match.matchID);
            Pooltem.Instance.ReturnBoardItem(itemObj);
            board.NormalGrid[pos.x, pos.y] = null;

            if (board.BoardCellGrid[pos.x, pos.y] != null &&
                board.BoardCellGrid[pos.x, pos.y].TryGetComponent<BoardCell>(out var cell))
            {
                cell.State = EnumStateBoardCell.Empty;
                cell.IsGettingFilled = false;
            }
        }
    }
}
