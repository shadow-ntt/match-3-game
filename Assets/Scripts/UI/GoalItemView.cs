using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Item UI don le hien thi icon, so luong con lai va checkmark hoan thanh cua 1 muc tieu
public class GoalItemView : MonoBehaviour
{
    [Header("UI Tham Chieu")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private GameObject checkmarkObj;

    private int itemId;
    private int requiredAmount;
    private bool isCompleted;
    private Tween punchTween;
    private Tween checkmarkTween;

    public int ItemId => itemId;
    public bool IsCompleted => isCompleted;

    private void OnDisable()
    {
        punchTween?.Kill();
        checkmarkTween?.Kill();
        // Reset scale ve mac dinh de tranh bi bien dang khi view bi disable
        if (amountText != null) amountText.transform.localScale = Vector3.one;
        if (checkmarkObj != null) checkmarkObj.transform.localScale = Vector3.one;
    }

    // Khoi tao muc tieu voi icon va so luong yeu cau
    public void Initialize(int id, Sprite icon, int required)
    {
        itemId = id;
        requiredAmount = required;
        isCompleted = false;

        punchTween?.Kill();
        checkmarkTween?.Kill();

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }

        if (amountText != null)
        {
            amountText.gameObject.SetActive(true);
            amountText.transform.localScale = Vector3.one;
            amountText.text = required.ToString();
        }

        if (checkmarkObj != null)
        {
            checkmarkObj.transform.localScale = Vector3.one;
            checkmarkObj.SetActive(false);
        }
    }

    // Cap nhat so luong da thu thap va hien thi checkmark khi dat yeu cau
    public void UpdateAmount(int current)
    {
        if (isCompleted) return;

        int remaining = Mathf.Max(0, requiredAmount - current);
        amountText.text = remaining.ToString();

        punchTween?.Kill();
        amountText.transform.localScale = Vector3.one;
        punchTween = amountText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 1, 0.5f);

        if (remaining <= 0)
        {
            isCompleted = true;
            amountText.gameObject.SetActive(false);

            if (checkmarkObj != null)
            {
                checkmarkTween?.Kill();
                checkmarkObj.SetActive(true);
                checkmarkObj.transform.localScale = Vector3.zero;
                checkmarkTween = checkmarkObj.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
        }
    }
}
