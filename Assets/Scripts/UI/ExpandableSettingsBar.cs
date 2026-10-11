using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Thanh cai dat bung nganh/doc: 1 nut chinh mo cac nut con voi huong va khoang cach tuy bien
public class ExpandableSettingsBar : MonoBehaviour
{
    public enum ExpandDirection
    {
        Left,   // Bung sang trai (Right to Left - dung goc duoi phai)
        Up,     // Bung len tren (Bottom to Top)
        Right,  // Bung sang phai (Left to Right)
        Down    // Bung xuong duoi (Top to Bottom)
    }

    [System.Serializable]
    public class SubButtonEntry
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public Button button;
    }

    [Header("Nut chinh (Toggle Settings)")]
    [SerializeField] private Button mainButton;
    [SerializeField] private RectTransform mainButtonIcon;

    [Header("Danh sach cac nut con")]
    [SerializeField] private SubButtonEntry[] subButtons;

    [Header("Huong va khoang cach")]
    [SerializeField] private ExpandDirection expandDirection = ExpandDirection.Left;
    [SerializeField] private float spaceStart = 110f;
    [SerializeField] private float buttonSpacing = 100f;

    [Header("Thong so hoat anh")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private float staggerDelay = 0.05f;
    [SerializeField] private float iconRotateAngle = 180f;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Sequence currentSequence;

    public bool IsOpen => isOpen;
    public float SpaceStart => spaceStart;
    public float ButtonSpacing => buttonSpacing;

    private void Awake()
    {
        // Khoi tao trang thai an ban dau cho cac nut con
        ResetSubButtonsToClosedState();
    }

    private void Start()
    {
        mainButton.onClick.AddListener(OnMainButtonClicked);
    }

    private void OnDestroy()
    {
        mainButton.onClick.RemoveListener(OnMainButtonClicked);

        currentSequence?.Kill();
    }

    // Tinh toa do muc tieu cho nut con theo chi so va huong bung
    private Vector2 GetTargetPosition(int index)
    {
        float offset = spaceStart + index * buttonSpacing;
        switch (expandDirection)
        {
            case ExpandDirection.Left:
                return new Vector2(-offset, 0f);
            case ExpandDirection.Up:
                return new Vector2(0f, offset);
            case ExpandDirection.Right:
                return new Vector2(offset, 0f);
            case ExpandDirection.Down:
                return new Vector2(0f, -offset);
            default:
                return new Vector2(-offset, 0f);
        }
    }

    // Dat lai vi tri va trang thai an cua tat ca cac nut con ve vi tri nut chinh
    private void ResetSubButtonsToClosedState()
    {
        for (int i = 0; i < subButtons.Length; i++)
        {
            SubButtonEntry entry = subButtons[i];
            entry.rectTransform.anchoredPosition = Vector2.zero;
            entry.rectTransform.localScale = Vector3.zero;
            entry.canvasGroup.alpha = 0f;
            entry.canvasGroup.blocksRaycasts = false;
            entry.canvasGroup.interactable = false;
        }

        if (mainButtonIcon != null)
        {
            mainButtonIcon.localRotation = Quaternion.identity;
        }

        isOpen = false;
        isAnimating = false;
    }

    // Xu ly click nut chinh: mo neu dang dong, dong neu dang mo
    private void OnMainButtonClicked()
    {
        if (isAnimating) return;

        mainButton.transform.DOKill();
        mainButton.transform.localScale = Vector3.one;
        mainButton.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 8, 1f);

        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    // Bung cac nut con theo huong da cau hinh
    public void Open()
    {
        if (isOpen || isAnimating) return;

        isAnimating = true;
        isOpen = true;

        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // Xoay icon nut chinh
        if (mainButtonIcon != null)
        {
            currentSequence.Join(mainButtonIcon.DORotate(new Vector3(0f, 0f, iconRotateAngle), animDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutBack));
        }

        // Bung lan luot cac nut con theo toa do Vector2 tinh toan
        for (int i = 0; i < subButtons.Length; i++)
        {
            SubButtonEntry entry = subButtons[i];
            Vector2 targetPos = GetTargetPosition(i);
            float delay = i * staggerDelay;

            entry.canvasGroup.blocksRaycasts = true;
            entry.canvasGroup.interactable = true;

            currentSequence.Insert(delay, entry.rectTransform.DOAnchorPos(targetPos, animDuration).SetEase(Ease.OutBack));
            currentSequence.Insert(delay, entry.rectTransform.DOScale(1f, animDuration).SetEase(Ease.OutBack));
            currentSequence.Insert(delay, entry.canvasGroup.DOFade(1f, animDuration * 0.7f));
        }

        currentSequence.OnComplete(() =>
        {
            isAnimating = false;
        });
    }

    // Thu gon cac nut con ve vi tri nut chinh (Vector2.zero)
    public void Close()
    {
        if (!isOpen || isAnimating) return;

        isAnimating = true;
        isOpen = false;

        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // Xoay icon nut chinh tro lai goc ban dau
        if (mainButtonIcon != null)
        {
            currentSequence.Join(mainButtonIcon.DORotate(Vector3.zero, animDuration, RotateMode.Fast)
                .SetEase(Ease.InQuad));
        }

        // Thu gon lan luot tu ngoai cung ve trong
        for (int i = subButtons.Length - 1; i >= 0; i--)
        {
            SubButtonEntry entry = subButtons[i];
            float delay = (subButtons.Length - 1 - i) * staggerDelay;

            entry.canvasGroup.blocksRaycasts = false;
            entry.canvasGroup.interactable = false;

            currentSequence.Insert(delay, entry.rectTransform.DOAnchorPos(Vector2.zero, animDuration * 0.85f).SetEase(Ease.InBack));
            currentSequence.Insert(delay, entry.rectTransform.DOScale(0f, animDuration * 0.85f).SetEase(Ease.InBack));
            currentSequence.Insert(delay, entry.canvasGroup.DOFade(0f, animDuration * 0.6f));
        }

        currentSequence.OnComplete(() =>
        {
            isAnimating = false;
        });
    }

    // Xu ly click nut Exit neu co gan
    private void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
