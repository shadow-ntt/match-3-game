using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Tia sang noi tu LightBall toi gem muc tieu truoc khi phat no
public class LightBallBeam : MonoBehaviour
{
    [Header("Beam Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float defaultWidth = 0.18f;
    [SerializeField] private int sortingOrder = 20;
    [SerializeField] private float zOffset = -0.1f;

    private Tween fadeTween;

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.sortingOrder = sortingOrder;
            lineRenderer.startWidth = defaultWidth;
            lineRenderer.endWidth = defaultWidth;
        }
    }

    // Ban tia sang tu diem from den diem to voi thoi gian va mau sac chi dinh
    public async UniTask ShootAsync(Vector3 from, Vector3 to, float duration, Color beamColor)
    {
        EnsureLineRenderer();
        if (lineRenderer == null) return;

        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }

        from.z += zOffset;
        to.z += zOffset;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.startWidth = defaultWidth;
        lineRenderer.endWidth = defaultWidth;

        Color startColor = beamColor;
        startColor.a = 1f;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = startColor;

        // Giu sang ro rang trong phan dau duration, sau do fade out va co dan chieu rong
        float holdDuration = duration * 0.4f;
        float fadeDuration = Mathf.Max(0.05f, duration - holdDuration);

        if (holdDuration > 0)
        {
            await UniTask.Delay((int)(holdDuration * 1000f));
        }

        if (this == null || lineRenderer == null) return;

        fadeTween = DOTween.To(() => 1f, progress =>
        {
            if (lineRenderer == null) return;
            Color c = beamColor;
            c.a = progress;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
            lineRenderer.startWidth = defaultWidth * progress;
            lineRenderer.endWidth = defaultWidth * progress;
        }, 0f, fadeDuration).SetEase(Ease.InQuad);

        await fadeTween.ToUniTask();
    }

    private void OnDisable()
    {
        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }
    }
}

