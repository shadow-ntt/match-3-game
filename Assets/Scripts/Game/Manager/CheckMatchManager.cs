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
                    ExplodeMatches(matches);

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

    // No tat ca cac cum match (Normal Match va Special Match)
    private void ExplodeMatches(List<MatchData> matches)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.MatchType == MatchType.Normal)
            {
                HandleNormalMatch(match);
            }
            else
            {
                HandleSpecialMatch(match);
            }
        }
    }

    // Xu ly Normal Match: phat particle no cho cac gem va xoa khoi grid
    private void HandleNormalMatch(MatchData match)
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
        }
    }

    // Xu ly Special Match: phat particle no cho cac gem, xoa khoi grid va spawn booster tai centerCell
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
