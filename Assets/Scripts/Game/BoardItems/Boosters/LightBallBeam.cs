using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Tia sang noi tu LightBall toi gem muc tieu truoc khi phat no
public class LightBallBeam : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private Tween fadeTween;

    private void Awake()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }

    // Ban tia sang tu diem from den diem to voi thoi gian va mau sac chi dinh
    public async UniTask ShootAsync(Vector3 from, Vector3 to, float duration, Color beamColor)
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) return;

        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);

        Color startColor = beamColor;
        startColor.a = 1f;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = startColor;

        fadeTween = DOTween.To(() => 1f, alpha =>
        {
            if (lineRenderer == null) return;
            Color c = beamColor;
            c.a = alpha;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
        }, 0f, duration).SetEase(Ease.InQuad);

        await fadeTween.ToUniTask();
    }
}
