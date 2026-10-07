using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly Input cu chi (Swipe/Drag) va hoan doi cac IBoardItem tren ban co Match-3
public class HandleInput : MonoBehaviour
{
    [Header("Tham chieu Ban Co & Quan ly Match")]
    [SerializeField] private Board board;
    [SerializeField] private CheckMatchManager checkMatchManager;
    [SerializeField] private BoosterActivationManager boosterActivationManager;
    [SerializeField] private Camera mainCamera;

    [Header("Cau hinh Cu chi Vuot")]
    [Tooltip("Nguong toi thieu pixel de tinh la 1 cu vuot (Deadzone)")]
    [SerializeField] private float swipeThreshold = 50f;
    [Tooltip("Bat/tat tiep nhan Input")]
    [SerializeField] private bool enableInput = true;

    [Header("Cau hinh Animation Swap (DOTween)")]
    [Tooltip("Thoi gian chuyen dong khi hoan doi")]
    [SerializeField] private float swapDuration = 0.2f;
    [Tooltip("Kieu Ease cua animation")]
    [SerializeField] private Ease swapEase = Ease.OutQuad;

    // Bien theo doi thao tac nguoi choi
    private IBoardItem _selectedItem;
    private Vector2 _startMousePos;
    private bool _isDragging;
    private bool isMovingSwap;

    public Board Board { get => board; set => board = value; }
    public bool EnableInput { get => enableInput; set => enableInput = value; }
    public bool IsMovingSwap { get => isMovingSwap; set => isMovingSwap = value; }

    private void Awake()
    {
        mainCamera = Camera.main;
        if (checkMatchManager == null) checkMatchManager = FindAnyObjectByType<CheckMatchManager>();
        if (boosterActivationManager == null) boosterActivationManager = FindAnyObjectByType<BoosterActivationManager>();
    }

    private void Update()
    {
        // Khi đang di chuyển hoán đổi (isMovingSwap) thì không cho nhận input chuột
        if (!enableInput || isMovingSwap || board == null || board.MidGrid == null) return;

        // Bắt đầu click/chạm vào viên ngọc
        if (Input.GetMouseButtonDown(0))
        {
            OnPointerDown();
        }
        // Khi đang giữ chuột: kiểm tra liên tục, vừa vượt ngưỡng swipeThreshold là swap ngay lập tức
        else if (Input.GetMouseButton(0) && _isDragging)
        {
            OnPointerDrag();
        }
        // Thả chuột/ngón tay
        else if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            _selectedItem = null;
        }
    }

    // Xu ly khi nhan chuot / cham vao man hinh
    private void OnPointerDown()
    {
        if (isMovingSwap) return;

        _startMousePos = Input.mousePosition;
        _selectedItem = GetItemAtScreenPosition(_startMousePos);

        if (_selectedItem != null && BoardItemUtils.CanSwap(_selectedItem))
        {
            _isDragging = true;
        }
        else
        {
            _selectedItem = null;
            _isDragging = false;
        }
    }

    // Kiem tra lien tuc khi dang giu chuot: vuot nguong la swap ngay
    private void OnPointerDrag()
    {
        if (isMovingSwap || _selectedItem == null) return;

        Vector2 currentMousePos = Input.mousePosition;
        if (HandleSwipe(_selectedItem, _startMousePos, currentMousePos))
        {
            _isDragging = false;
            _selectedItem = null;
        }
    }

    // Xu ly vuot: tinh huong tu InputUtils va hoan doi voi item lan can
    public bool HandleSwipe(IBoardItem sourceItem, Vector2 startPos, Vector2 endPos)
    {
        if (isMovingSwap) return false;

        Vector2 dir = InputUtils.GetSwipeDirection(startPos, endPos, swipeThreshold);
        return dir != Vector2.zero && HandleSwipe(sourceItem, dir);
    }

    // Xu ly vuot truc tiep tu huong Vector2
    public bool HandleSwipe(IBoardItem sourceItem, Vector2 swipeDirection)
    {
        if (isMovingSwap) return false;
        if (!TryGetItemCoordinates(sourceItem, out int x, out int y)) return false;

        Vector2Int offset = InputUtils.SwipeDirectionToOffset(swipeDirection);
        Vector2Int targetPos = new Vector2Int(x + offset.x, y + offset.y);

        if (!board.IsInBounds(targetPos)) return false;

        var targetItem = board.MidGrid[targetPos.x, targetPos.y]?.GetComponent<IBoardItem>();
        return targetItem != null && Swap(sourceItem, targetItem);
    }

    // Hoan doi vi tri giua 2 IBoardItem bang DOTween
    public bool Swap(IBoardItem itemA, IBoardItem itemB)
    {
        if (isMovingSwap || board == null || board.MidGrid == null) return false;
        if (itemA == null || itemB == null) return false;

        if (!BoardItemUtils.CanSwap(itemA, itemB)) return false;

        if (!TryGetItemCoordinates(itemA, out int xA, out int yA) ||
            !TryGetItemCoordinates(itemB, out int xB, out int yB))
        {
            return false;
        }

        // 1. Hoan doi trong ma tran MidGrid
        GameObject objA = board.MidGrid[xA, yA];
        GameObject objB = board.MidGrid[xB, yB];

        board.MidGrid[xA, yA] = objB;
        board.MidGrid[xB, yB] = objA;

        // 2. Hoan doi vi tri Transform bang DOTween va kiem tra match bang UniTask
        if (objA != null && objB != null)
        {
            isMovingSwap = true;
            AnimateSwapAndCheck(objA, objB, xA, yA, xB, yB).Forget();
        }

        return true;
    }

    // Hoat anh hoan doi va kiem tra match / kich hoat booster / no lien hoan bang UniTask
    private async UniTaskVoid AnimateSwapAndCheck(GameObject objA, GameObject objB, int xA, int yA, int xB, int yB)
    {
        try
        {
            Vector3 posA = objA.transform.position;
            Vector3 posB = objB.transform.position;

            var tweenA = objA.transform.DOMove(posB, swapDuration).SetEase(swapEase);
            var tweenB = objB.transform.DOMove(posA, swapDuration).SetEase(swapEase);
            await UniTask.WhenAll(tweenA.ToUniTask(), tweenB.ToUniTask());

            var itemA = objA.GetComponent<IBoardItem>();
            var itemB = objB.GetComponent<IBoardItem>();

            bool isBoosterA = BoardItemUtils.IsBoosterItem(itemA);
            bool isBoosterB = BoardItemUtils.IsBoosterItem(itemB);
            bool activated = false;

            var activationMgr = boosterActivationManager != null ? boosterActivationManager : BoosterActivationManager.Instance;

            // Kich hoat Booster neu co booster tham gia
            if (isBoosterA && isBoosterB && activationMgr != null)
            {
                // Ca hai deu la booster -> Kich hoat hieu ung combo
                activated = await activationMgr.ActivateBoosterComboAsync(xB, yB, itemA, xA, yA, itemB);
            }
            else if (isBoosterA && !isBoosterB && activationMgr != null)
            {
                // objA (booster) da di chuyen sang (xB, yB), itemB la target bi swap vao
                activated = await activationMgr.ActivateBoosterAsync(xB, yB, itemB);
            }
            else if (isBoosterB && !isBoosterA && activationMgr != null)
            {
                // objB (booster) da di chuyen sang (xA, yA), itemA la target bi swap vao
                activated = await activationMgr.ActivateBoosterAsync(xA, yA, itemA);
            }

            if (activated)
            {
                // Sau khi booster no, cho cac ngoc con lai roi xuong lap khoang trong
                if (ItemFallManager.Instance != null)
                {
                    await ItemFallManager.Instance.OnItemFallAsync();
                }

                // Kiem tra cascade xem cac ngoc roi xuong co tao match moi khong
                bool hasMatch = checkMatchManager != null && await checkMatchManager.CheckMatch();
                if (!hasMatch)
                {
                    // Neu chua tao match, sinh ngoc moi tu tren troi roi xuong de lap day ban co
                    if (SpawnItemFromSky.Instance != null)
                    {
                        await SpawnItemFromSky.Instance.SpawnFromSkyAsync();
                    }
                    if (ItemFallManager.Instance != null)
                    {
                        await ItemFallManager.Instance.OnItemFallAsync();
                    }
                    // Kiem tra match sau khi ngoc moi da roi vao vi tri
                    if (checkMatchManager != null)
                    {
                        await checkMatchManager.CheckMatch();
                    }
                }
            }
            else
            {
                // Kiem tra Match thuong sau khi hoan doi, neu khong co thi hoan lai 2 vi tri
                Vector2Int priorityCenter = new Vector2Int(xB, yB);
                bool matched = checkMatchManager != null && await checkMatchManager.CheckMatch(priorityCenter);

                if (!matched)
                {
                    board.MidGrid[xA, yA] = objA;
                    board.MidGrid[xB, yB] = objB;

                    var backA = objA.transform.DOMove(posA, swapDuration).SetEase(swapEase);
                    var backB = objB.transform.DOMove(posB, swapDuration).SetEase(swapEase);
                    await UniTask.WhenAll(backA.ToUniTask(), backB.ToUniTask());
                }
            }
        }
        finally
        {
            isMovingSwap = false;
        }
    }

    // Tim toa do (x, y) cua mot IBoardItem trong MidGrid
    public bool TryGetItemCoordinates(IBoardItem item, out int x, out int y)
    {
        x = -1;
        y = -1;

        if (item == null || board == null || board.MidGrid == null) return false;

        Component comp = item as Component;
        if (comp == null) return false;

        Vector2Int gridPos = GridUtils.WorldToGrid(board.Grid, comp.transform.position);
        if (board.IsInBounds(gridPos) && board.MidGrid[gridPos.x, gridPos.y] == comp.gameObject)
        {
            x = gridPos.x;
            y = gridPos.y;
            return true;
        }

        return false;
    }

    // Lay IBoardItem tai toa do man hinh (Screen Position)
    private IBoardItem GetItemAtScreenPosition(Vector2 screenPos)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        Vector2Int gridPos = GridUtils.WorldToGrid(board.Grid, worldPos);

        if (board.IsInBounds(gridPos))
        {
            GameObject obj = board.MidGrid[gridPos.x, gridPos.y];
            if (obj != null)
            {
                return obj.GetComponent<IBoardItem>();
            }
        }

        return null;
    }
}
