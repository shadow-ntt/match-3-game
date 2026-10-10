using DG.Tweening;
using TMPro;
using UnityEngine;

// Hien thi so luot di chuyen con lai kem canh bao mau do khi sap het luot
public class MovesView : MonoBehaviour
{
    [Header("UI Tham Chieu")]
    [SerializeField] private TextMeshProUGUI movesText;

    [Header("Cau Hinh Canh Bao")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private int warningThreshold = 5;
    [SerializeField] private float warningPunchScale = 0.2f;

    private Tween punchTween;

    private void OnEnable()
    {
        GameEventBus.OnMovesChanged += OnMovesChangedHandler;
    }

    private void OnDisable()
    {
        GameEventBus.OnMovesChanged -= OnMovesChangedHandler;
        punchTween?.Kill();
    }

    // Xu ly khi so luot di thay doi tu GameEventBus
    private void OnMovesChangedHandler(int movesLeft, int totalMoves)
    {
        movesText.text = movesLeft.ToString();

        punchTween?.Kill();
        movesText.transform.localScale = Vector3.one;

        if (movesLeft <= warningThreshold && movesLeft > 0)
        {
            movesText.color = warningColor;
            punchTween = movesText.transform.DOPunchScale(Vector3.one * warningPunchScale, 0.25f, 2, 0.5f);
        }
        else if (movesLeft == 0)
        {
            movesText.color = warningColor;
            punchTween = movesText.transform.DOShakePosition(0.3f, 5f, 10, 90f);
        }
        else
        {
            movesText.color = normalColor;
            punchTween = movesText.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 1, 0.5f);
        }
    }
}
