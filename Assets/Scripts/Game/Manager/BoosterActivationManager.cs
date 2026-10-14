using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

// Quan ly kich hoat hieu ung va no day chuyen cac Booster tren ban co
public class BoosterActivationManager : Singleton<BoosterActivationManager>
{
    [SerializeField] private Board board;
    [SerializeField] private PoolBlockBreakEffect poolParticle;
    [SerializeField] private float explosionDelay = 0.25f;

    private BoosterEffectPlayer _effectPlayer;
    private BoosterCombos _combos;

    // Singleton Fields khoi tao tai Start
    private GoalTracker goalTracker;
    private ScoreManager scoreManager;
    private CameraShakeService cameraShakeService;

    public Board Board => board;
    public PoolBlockBreakEffect PoolParticle => poolParticle;
    public BoosterEffectPlayer EffectPlayer => _effectPlayer;
    public BoosterCombos Combos => _combos;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        _combos = new BoosterCombos();
        _effectPlayer = new BoosterEffectPlayer(this);
    }

    private void Start()
    {
        goalTracker = GoalTracker.Instance;
        scoreManager = ScoreManager.Instance;
        cameraShakeService = CameraShakeService.Instance;
    }

    // Kich hoat booster tai (x, y), ho tro kich hoat day chuyen (Chain Reaction) bang vong lap Queue
    // Tra ve true neu booster kich hoat thanh cong
    public async UniTask<bool> ActivateBoosterAsync(
        int x,
        int y,
        IBoardItem swapTarget = null,
        HashSet<Vector2Int> visited = null)
    {
        visited ??= new HashSet<Vector2Int>();
        var pos = new Vector2Int(x, y);
        visited.Add(pos);

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(pos);

        return await ProcessBoosterQueueAsync(queue, visited, swapTarget);
    }

    // Kich hoat hieu ung combo giua 2 booster tai (xA, yA) va (xB, yB), ho tro day chuyen
    public async UniTask<bool> ActivateBoosterComboAsync(int xA, int yA, IBoardItem boosterA, int xB, int yB, IBoardItem boosterB)
    {
        var combo = _combos.GetCombo(boosterA.ItemId, boosterB.ItemId);
        if (combo == null) return false;

        var visited = new HashSet<Vector2Int>
        {
            new Vector2Int(xA, yA),
            new Vector2Int(xB, yB)
        };

        // Tam combo lay tai vi tri vuot den (xB, yB)
        int cx = xB;
        int cy = yB;

        GameEventBus.RaiseBoosterComboActivated(boosterA.ItemId, boosterB.ItemId);

        // Lay danh sach cac o bi anh huong boi combo
        var affectedCells = combo.GetAffectedCells(board, cx, cy, boosterA.ItemId, boosterB.ItemId);

        GameObject objA = board.MidGrid[xA, yA];
        GameObject objB = board.MidGrid[xB, yB];
        Vector3 centerPos = objB != null ? objB.transform.position : GridUtils.GridToWorld(board.Grid, cx, cy);

        // Phat animation hop nhat combo truoc khi xoa
        await combo.PlayMergeAnimationAsync(objA, objB, centerPos);

        // Rung camera neu combo co cau hinh
        if (combo.ShakeType == CameraShakeType.Mega)
        {
            cameraShakeService.ShakeMega().Forget();
        }
        else if (combo.ShakeType == CameraShakeType.TNT)
        {
            cameraShakeService.ShakeTNT().Forget();
        }

        // Xoa 2 vien booster tham gia combo truoc
        goalTracker.RegisterDestroyed((int)boosterA.ItemId);
        goalTracker.RegisterDestroyed((int)boosterB.ItemId);

        scoreManager.AddScore(2, isObstacle: false, isByBooster: true);

        RemoveItem(xA, yA);
        RemoveItem(xB, yB);

        // Gom cac booster khac nam trong vung no de kich hoat day chuyen vao hang doi
        var chainQueue = new Queue<Vector2Int>();
        var chainBoosters = CollectAndEnqueueChainBoosters(affectedCells, visited, chainQueue);

        // Dong goi ngu canh va thuc thi hieu ung combo
        var context = new BoosterComboContext(
            board,
            this,
            cx, cy,
            xA, yA, xB, yB,
            boosterA.ItemId, boosterB.ItemId,
            affectedCells,
            chainBoosters,
            visited
        );

        await combo.ExecuteEffectAsync(context);

        // Kich hoat day chuyen cac booster con lai bang vong lap Queue
        await ProcessBoosterQueueAsync(chainQueue, visited);

        return true;
    }

    // Xu ly toan bo hang doi booster va chain reaction bang vong lap while
    private async UniTask<bool> ProcessBoosterQueueAsync(
        Queue<Vector2Int> queue,
        HashSet<Vector2Int> visited,
        IBoardItem initialSwapTarget = null)
    {
        bool anyActivated = false;
        IBoardItem currentSwapTarget = initialSwapTarget;

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();

            bool activated = await ActivateSingleBoosterAsync(
                pos.x,
                pos.y,
                currentSwapTarget,
                visited,
                queue
            );

            if (activated)
            {
                anyActivated = true;
            }

            // swapTarget chi danh rieng cho booster ban dau duoc nguoi choi swap
            currentSwapTarget = null;
        }

        return anyActivated;
    }

    // Ham noi bo thuc hien kich hoat mot booster don le
    private async UniTask<bool> ActivateSingleBoosterAsync(
        int x,
        int y,
        IBoardItem swapTarget,
        HashSet<Vector2Int> visited,
        Queue<Vector2Int> chainQueue)
    {
        GameObject boosterObj = board.MidGrid[x, y];
        if (boosterObj == null || !boosterObj.TryGetComponent<IBoosterActivatable>(out var booster))
            return false;

        // Lay danh sach cac o bi anh huong
        var affectedCells = booster.GetAffectedCells(board, x, y, swapTarget);

        // Phat animation kich hoat cua chinh booster truoc khi xoa
        await booster.PlayActivationAnimationAsync();

        // Ghi nhan booster bi pha huy truoc khi xoa khoi ban co
        if (booster is IBoardItem bItem)
        {
            goalTracker.RegisterDestroyed((int)bItem.ItemId);
            GameEventBus.RaiseBoosterActivated(bItem.ItemId);
        }

        scoreManager.AddScore(1, isObstacle: false, isByBooster: true);

        // Xoa ban than booster khoi ban co
        RemoveItem(x, y);

        // Gom cac booster khac nam trong vung no de kich hoat day chuyen va day vao hang doi
        var chainBoosters = CollectAndEnqueueChainBoosters(affectedCells, visited, chainQueue);

        // Phat animation projectile / blast va pha huy cac o qua da hinh (Polymorphism)
        var context = new BoosterActivationContext(
            board, this, x, y, affectedCells, chainBoosters, visited, swapTarget);
        await booster.ExecuteActivationEffectAsync(context);

        return true;
    }

    // Pha huy va xoa item tai 1 o tren ban co (qua ItemDestructionHandler tap trung)
    public void ExplodeAndRemoveCell(int cx, int cy)
    {
        ItemDestructionHandler.ExplodeByBooster(board, cx, cy);
    }

    // Xoa 1 item khoi MidGrid va cap nhat trang thai BoardCell ve Empty
    private void RemoveItem(int x, int y) => board.RemoveMidItem(x, y);

    // Cho vu no hoan tat theo thoi gian delay cau hinh
    public UniTask DelayExplosionAsync(float multiplier = 1f)
    {
        float delay = explosionDelay * multiplier;
        return delay > 0f
            ? UniTask.Delay(System.TimeSpan.FromSeconds(delay))
            : UniTask.CompletedTask;
    }

    // Gom cac booster chua tham gia no trong vung anh huong va day vao hang doi chain
    private List<Vector2Int> CollectAndEnqueueChainBoosters(
        List<Vector2Int> affectedCells,
        HashSet<Vector2Int> visited,
        Queue<Vector2Int> queue)
    {
        var chainBoosters = new List<Vector2Int>();
        if (affectedCells == null || board.MidGrid == null) return chainBoosters;

        for (int i = 0; i < affectedCells.Count; i++)
        {
            Vector2Int pos = affectedCells[i];
            if (!board.IsInBounds(pos.x, pos.y) || visited.Contains(pos)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj != null && obj.TryGetComponent<IBoosterActivatable>(out _))
            {
                visited.Add(pos);
                chainBoosters.Add(pos);
                queue.Enqueue(pos);
            }
        }
        return chainBoosters;
    }
}
