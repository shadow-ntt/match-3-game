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

    // Kiem tra match tren ban co va xu ly chuoi no lien hoan (Cascade / Combo)
    // Quy tac: Khi ngoc roi xuong thi kiem tra match, het match moi duoc phep sinh ngoc moi tu tren troi
    public async UniTask<bool> CheckMatch(Vector2Int? priorityCenter = null)
    {
        var initialMatches = _matchChecker.FindAllMatches(priorityCenter);
        if (initialMatches.Count == 0) return false;

        _isProcessing = true;
        Vector2Int? currentCenter = priorityCenter;

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

                    // Nổ tất cả các match
                    ExplodeMatches(matches);

                    // Chờ hiệu ứng hạt nổ
                    if (explosionDelay > 0f)
                    {
                        await UniTask.Delay(System.TimeSpan.FromSeconds(explosionDelay));
                    }

                    // Cho các ngọc hiện có rơi xuống lấp khoảng trống
                    if (ItemFallManager.Instance != null)
                    {
                        await ItemFallManager.Instance.OnItemFallAsync();
                    }

                    // Sau lượt nổ đầu tiên thì các lượt nổ tiếp theo không còn tâm ưu tiên
                    currentCenter = null;
                }

                // PHA 2: CHỈ SINH NGỌC MỚI TỪ TRỜI KHI ĐÃ HẾT MATCH TRÊN BÀN CỜ
                bool spawned = false;
                if (SpawnItemFromSky.Instance != null)
                {
                    spawned = await SpawnItemFromSky.Instance.SpawnFromSkyAsync();
                }

                if (!spawned)
                {
                    // Bàn cờ đã đầy hoặc không còn ô nào spawn được nữa, và Pha 1 đã xác nhận hết match
                    break;
                }

                // Cho ngọc mới rơi ổn định nếu có khoảng trống bổ sung (ví dụ rơi chéo)
                if (ItemFallManager.Instance != null)
                {
                    await ItemFallManager.Instance.OnItemFallAsync();
                }

                // Sau khi ngọc mới rơi xuống, kiểm tra xem có tạo match mới hay không
                var matchesAfterSpawn = _matchChecker.FindAllMatches(null);
                if (matchesAfterSpawn.Count == 0)
                {
                    // Ngọc sinh ra không tạo match mới và bàn cờ đã được lấp đầy
                    break;
                }
            }

            return true;
        }
        finally
        {
            _isProcessing = false;
        }
    }

    // No tat ca cac match trong danh sach
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

    // Xu ly MatchType.Normal: phat particle no, tra item ve pool va xoa khoi grid
    private void HandleNormalMatch(MatchData match)
    {
        for (int j = 0; j < match.Matches.Count; j++)
        {
            Vector2Int pos = match.Matches[j];
            GameObject itemObj = board.MidGrid[pos.x, pos.y];
            if (itemObj == null) continue;

            poolParticle.Play(itemObj.transform.position, (EnumItemBoard)match.matchID);

            Pooltem.Instance.ReturnBoardItem(itemObj);
            board.MidGrid[pos.x, pos.y] = null;

            if (board.BoardCellGrid[pos.x, pos.y] != null &&
                board.BoardCellGrid[pos.x, pos.y].TryGetComponent<BoardCell>(out var cell))
            {
                cell.State = EnumStateBoardCell.Empty;
                cell.IsGettingFilled = false;
            }
        }
    }

    // Xu ly Special Match: phat particle no cho cac gem, xoa khoi grid va spawn booster tai centerCell
    private void HandleSpecialMatch(MatchData match)
    {
        Vector2Int center = match.centerCell;
        EnumItemBoard boosterId = GetBoosterIdForMatch(match.MatchType);

        // Xoa toan bo cac gem trong cum match
        for (int j = 0; j < match.Matches.Count; j++)
        {
            Vector2Int pos = match.Matches[j];
            GameObject itemObj = board.MidGrid[pos.x, pos.y];
            if (itemObj == null) continue;

            poolParticle.Play(itemObj.transform.position, (EnumItemBoard)match.matchID);

            Pooltem.Instance.ReturnBoardItem(itemObj);
            board.MidGrid[pos.x, pos.y] = null;

            if (board.BoardCellGrid[pos.x, pos.y] != null &&
                board.BoardCellGrid[pos.x, pos.y].TryGetComponent<BoardCell>(out var cell))
            {
                cell.State = EnumStateBoardCell.Empty;
                cell.IsGettingFilled = false;
            }
        }

        // Sinh booster tai centerCell neu ID hop le
        if (boosterId == EnumItemBoard.Blank) return;

        Vector3 worldPos = GridUtils.GridToWorld(board.Grid, center.x, center.y);
        Transform parent = board.MidTilemap != null ? board.MidTilemap.transform : board.transform;
        GameObject boosterObj = Pooltem.Instance.SpawnBoardItem((int)boosterId, worldPos, Quaternion.identity, parent);
        if (boosterObj == null) return;

        board.MidGrid[center.x, center.y] = boosterObj;

        if (board.BoardCellGrid[center.x, center.y] != null &&
            board.BoardCellGrid[center.x, center.y].TryGetComponent<BoardCell>(out var boosterCell))
        {
            boosterCell.State = EnumStateBoardCell.Occupied;
            boosterCell.IsGettingFilled = false;
        }
    }

    // Lay EnumItemBoard tuong ung cho booster theo MatchType
    private static EnumItemBoard GetBoosterIdForMatch(MatchType matchType)
    {
        switch (matchType)
        {
            case MatchType.HorizontalRocket:
                return EnumItemBoard.HorizontalRocket;
            case MatchType.VerticalRocket:
                return EnumItemBoard.VerticalRocket;
            case MatchType.TNT:
                return EnumItemBoard.TNT;
            case MatchType.Missile:
                return EnumItemBoard.Missile;
            case MatchType.LightBall:
                return EnumItemBoard.LightBall;
            default:
                return EnumItemBoard.Blank;
        }
    }
}
