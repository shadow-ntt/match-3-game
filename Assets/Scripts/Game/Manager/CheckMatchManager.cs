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

    // Singleton Fields khoi tao tai Start
    private ScoreManager scoreManager;
    private GoalTracker goalTracker;
    private ItemFallManager itemFallManager;
    private SpawnItemFromSky spawnItemFromSky;
    private Pooltem pooltem;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;
    public bool IsProcessing => _isProcessing;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        _matchChecker = new LineScanMatchChecker(board);
        scoreManager = ScoreManager.Instance;
        goalTracker = GoalTracker.Instance;
        itemFallManager = ItemFallManager.Instance;
        spawnItemFromSky = SpawnItemFromSky.Instance;
        pooltem = Pooltem.Instance;
    }

    // Kiem tra nhanh co match tren ban co hay khong
    public bool HasMatches(Vector2Int? priorityCenter = null)
    {
        return _matchChecker.FindAllMatches(priorityCenter).Count > 0;
    }

    // Kiem tra match tren ban co va xu ly chuoi no lien hoan (Cascade / Combo)
    // Quy tac: Khi ngoc roi xuong thi kiem tra match, het match moi duoc phep sinh ngoc moi tu tren troi
    public async UniTask<bool> CheckMatch(Vector2Int? priorityCenter = null)
    {
        var initialMatches = _matchChecker.FindAllMatches(priorityCenter);
        if (initialMatches.Count == 0) return false;

        _isProcessing = true;
        Vector2Int? currentCenter = priorityCenter;

        scoreManager.ResetCombo();
        int cascadeStep = 0;

        try
        {
            while (true)
            {
                // PHA 1: NỔ & RƠI LIÊN HOÀN CÁC NGỌC TRÊN BÀN (HẾT SẠCH MATCH MỚI THÔI)

                while (true)
                {
                    var matches = _matchChecker.FindAllMatches(currentCenter);
                    if (matches.Count == 0)
                    {
                        // Đã hết sạch match giữa các viên ngọc hiện có trên bàn cờ
                        break;
                    }

                    if (cascadeStep > 0)
                    {
                        scoreManager.IncrementCombo();
                    }
                    cascadeStep++;

                    // Nổ tất cả các match
                    ExplodeMatches(matches, cascadeStep);

                    // Chờ hiệu ứng hạt nổ
                    if (explosionDelay > 0f)
                    {
                        await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
                    }

                    // Cho các ngọc hiện có rơi xuống lấp khoảng trống
                    await itemFallManager.OnItemFallAsync();

                    // Sau lượt nổ đầu tiên thì các lượt nổ tiếp theo không còn tâm ưu tiên
                    currentCenter = null;
                }

                // PHA 2: SINH NGỌC MỚI TỪ TRỜI RƠI XUỐNG ĐỂ LẤP ĐẦY BÀN CỜ
                // Chỉ sinh từ trên trời khi toàn bộ ngọc hiện có đã rơi hết và không còn tạo match nào

                bool anySpawned = await spawnItemFromSky.SpawnFromSkyAsync();

                if (!anySpawned)
                {
                    // Không có thêm ô nào được sinh mới -> bàn cờ đã đầy và ổn định
                    break;
                }

                // Sau khi sinh ngoc moi, cho cac ngoc roi xuong vi tri
                await itemFallManager.OnItemFallAsync();

                // Sau khi ngoc moi roi xong, vong lap se quay lai kiem tra match
                // Neu ngoc moi roi xuong tao thanh match moi -> lap lai PHA 1 (Cascade tiep tuc)
                // Neu ngoc moi roi xuong khong tao match -> FindAllMatches tra ve 0 -> thoat vong lap
            }

            return true;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    // 4 huong truc giao (Tren, Duoi, Trai, Phai) de tim cac o vat can o ria cum match
    private static readonly Vector2Int[] AdjacentDirections = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    // No tat ca cac cum match (Normal Match va Special Match) va pha huy vat can o ria
    private void ExplodeMatches(List<MatchData> matches, int cascadeStep)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.MatchType == MatchType.Normal)
            {
                HandleNormalMatch(match, cascadeStep);
            }
            else
            {
                HandleSpecialMatch(match);
            }
        }

        // Pha huy cac vat can o ria cua tat ca cac cum match
        ExplodeAdjacentObstacles(matches);
    }

    // Pha huy cac vat can o ria (4 huong truc giao) cua tat ca cac cum match
    private void ExplodeAdjacentObstacles(List<MatchData> matches)
    {
        if (matches == null || matches.Count == 0 || board == null) return;

        // 1. Thu thap tat ca cac toa do cua toan bo cac vien ngoc tham gia match trong dot nay
        var matchedPositions = new HashSet<Vector2Int>();
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.Matches == null) continue;
            for (int j = 0; j < match.Matches.Count; j++)
            {
                matchedPositions.Add(match.Matches[j]);
            }
        }

        // 2. Thu thap tat ca cac toa do o ria duy nhat (khong trung lap)
        var adjacentPositions = new HashSet<Vector2Int>();
        foreach (var pos in matchedPositions)
        {
            for (int d = 0; d < AdjacentDirections.Length; d++)
            {
                Vector2Int neighbor = pos + AdjacentDirections[d];
                // Bo qua neu ngoai ban co hoac chinh la mot o trong cum match
                if (!board.IsInBounds(neighbor.x, neighbor.y)) continue;
                if (matchedPositions.Contains(neighbor)) continue;

                adjacentPositions.Add(neighbor);
            }
        }

        // 3. Kiem tra va pha huy vat can tai tung toa do ria
        foreach (var pos in adjacentPositions)
        {
            TryExplodeObstacleAt(pos.x, pos.y);
        }
    }

    // Kiem tra va pha huy vat can tai o (x, y) theo thu tu uu tien 4 layer
    private bool TryExplodeObstacleAt(int x, int y)
    {
        if (!board.IsInBounds(x, y)) return false;

        // 1. TANG OVERLAY: Uu tien hang dau pha huy vat can tren OverlayGrid (da, bang tuyet, xich...)
        if (TryExplodeOverlay(x, y)) return true;

        // 2. TANG MID: Pha huy vat can tren MidGrid neu la obstacle (vi du: hom go, da giua ban co)
        if (TryExplodeMidObstacle(x, y)) return true;

        // 3. TANG UNDER: Pha huy vat can tren UnderGrid (ngoai tru Spawner)
        if (TryExplodeUnder(x, y)) return true;

        return false;
    }

    // Pha huy vat can tang OverlayGrid tai o (x, y)
    private bool TryExplodeOverlay(int x, int y)
    {
        if (board.OverlayGrid == null) return false;
        GameObject overlayObj = board.OverlayGrid[x, y];
        if (overlayObj == null) return false;

        int itemId = overlayObj.TryGetComponent<IBoardItem>(out var it) ? (int)it.ItemId : (int)EnumItemBoard.Blank;

        Vector3 worldPos = overlayObj.transform.position;
        if (poolParticle != null)
        {
            poolParticle.Play(worldPos, EnumItemColor.Red);
        }

        board.RemoveOverlayItem(x, y);

        if (itemId > 0)
        {
            if (goalTracker != null)
            {
                goalTracker.RegisterDestroyed(itemId);
            }
            if (BoardItemUtils.IsObstacle(itemId))
            {
                GameEventBus.RaiseObstacleDestroyed((EnumItemBoard)itemId);
            }
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(1, isObstacle: true, isByBooster: false);
        }

        return true;
    }

    // Pha huy vat can tang MidGrid tai o (x, y) neu la obstacle
    private bool TryExplodeMidObstacle(int x, int y)
    {
        if (board.MidGrid == null) return false;
        GameObject midObj = board.MidGrid[x, y];
        if (midObj == null) return false;

        if (!BoardItemUtils.IsObstacle(midObj)) return false;

        int itemId = midObj.TryGetComponent<IBoardItem>(out var it) ? (int)it.ItemId : (int)EnumItemBoard.Blank;

        Vector3 worldPos = midObj.transform.position;
        if (poolParticle != null)
        {
            poolParticle.Play(worldPos, EnumItemColor.Red);
        }

        board.RemoveMidItem(x, y);

        if (itemId > 0)
        {
            if (goalTracker != null)
            {
                goalTracker.RegisterDestroyed(itemId);
            }
            GameEventBus.RaiseObstacleDestroyed((EnumItemBoard)itemId);
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(1, isObstacle: true, isByBooster: false);
        }

        return true;
    }

    // Pha huy vat can tang UnderGrid tai o (x, y) neu khong phai Spawner
    private bool TryExplodeUnder(int x, int y)
    {
        if (board.UnderGrid == null) return false;
        GameObject underObj = board.UnderGrid[x, y];
        if (underObj == null) return false;

        // Spawner khong the bi pha huy
        if (underObj.TryGetComponent<IBoardItem>(out var underItem) && underItem.ItemId == EnumItemBoard.Spawn)
        {
            return false;
        }

        int itemId = underItem != null ? (int)underItem.ItemId : (int)EnumItemBoard.Blank;

        Vector3 worldPos = underObj.transform.position;
        if (poolParticle != null)
        {
            poolParticle.Play(worldPos, EnumItemColor.Red);
        }

        board.RemoveUnderItem(x, y);

        if (itemId > 0)
        {
            if (goalTracker != null)
            {
                goalTracker.RegisterDestroyed(itemId);
            }
            if (BoardItemUtils.IsObstacle(itemId))
            {
                GameEventBus.RaiseObstacleDestroyed((EnumItemBoard)itemId);
            }
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(1, isObstacle: true, isByBooster: false);
        }

        return true;
    }

    // Xu ly Normal Match: phat particle no cho cac gem va xoa khoi grid
    private void HandleNormalMatch(MatchData match, int cascadeStep)
    {
        int count = 0;
        for (int j = 0; j < match.Matches.Count; j++)
        {
            Vector2Int pos = match.Matches[j];
            GameObject itemObj = board.MidGrid[pos.x, pos.y];
            if (itemObj == null) continue;

            poolParticle.Play(itemObj.transform.position, (EnumItemBoard)match.matchID);

            pooltem.ReturnBoardItem(itemObj);
            board.MidGrid[pos.x, pos.y] = null;

            if (board.BoardCellGrid[pos.x, pos.y] != null &&
                board.BoardCellGrid[pos.x, pos.y].TryGetComponent<BoardCell>(out var cell))
            {
                cell.State = EnumStateBoardCell.Empty;
                cell.IsGettingFilled = false;
            }
            count++;
        }

        if (count > 0)
        {
            scoreManager.AddScore(count, isObstacle: false, isByBooster: false);
            goalTracker.RegisterDestroyed(match.matchID, count);
            GameEventBus.RaiseMatchExploded(count, cascadeStep);
        }
    }

    // Xu ly Special Match (tao booster): phat particle no cho cac gem, xoa khoi grid va spawn booster tai centerCell
    private void HandleSpecialMatch(MatchData match)
    {
        Vector2Int center = match.centerCell;
        EnumItemBoard boosterId = GetBoosterIdForMatch(match.MatchType);

        int count = 0;
        // Xoa toan bo cac gem trong cum match
        for (int j = 0; j < match.Matches.Count; j++)
        {
            Vector2Int pos = match.Matches[j];
            GameObject itemObj = board.MidGrid[pos.x, pos.y];
            if (itemObj == null) continue;

            poolParticle.Play(itemObj.transform.position, (EnumItemBoard)match.matchID);

            pooltem.ReturnBoardItem(itemObj);
            board.MidGrid[pos.x, pos.y] = null;

            if (board.BoardCellGrid[pos.x, pos.y] != null &&
                board.BoardCellGrid[pos.x, pos.y].TryGetComponent<BoardCell>(out var cell))
            {
                cell.State = EnumStateBoardCell.Empty;
                cell.IsGettingFilled = false;
            }
            count++;
        }

        if (count > 0)
        {
            scoreManager.AddScore(count, isObstacle: false, isByBooster: false);
            goalTracker.RegisterDestroyed(match.matchID, count);
        }

        // Sinh booster tai centerCell neu ID hop le
        if (boosterId == EnumItemBoard.Blank) return;

        Vector3 worldPos = GridUtils.GridToWorld(board.Grid, center.x, center.y);
        Transform parent = board.MidTilemap != null ? board.MidTilemap.transform : board.transform;
        GameObject boosterObj = pooltem.SpawnBoardItem((int)boosterId, worldPos, Quaternion.identity, parent);
        if (boosterObj == null) return;

        GameEventBus.RaiseBoosterCreated(boosterId);

        board.MidGrid[center.x, center.y] = boosterObj;

        if (board.BoardCellGrid[center.x, center.y] != null &&
            board.BoardCellGrid[center.x, center.y].TryGetComponent<BoardCell>(out var cellComp))
        {
            cellComp.State = EnumStateBoardCell.Occupied;
            cellComp.IsGettingFilled = false;
        }
    }

    // Anh xa MatchType sang ID booster tuong ung
    private EnumItemBoard GetBoosterIdForMatch(MatchType matchType)
    {
        return matchType switch
        {
            MatchType.HorizontalRocket => EnumItemBoard.HorizontalRocket,
            MatchType.VerticalRocket => EnumItemBoard.VerticalRocket,
            MatchType.TNT => EnumItemBoard.TNT,
            MatchType.Missile => EnumItemBoard.Missile,
            MatchType.LightBall => EnumItemBoard.LightBall,
            _ => EnumItemBoard.Blank
        };
    }
}
