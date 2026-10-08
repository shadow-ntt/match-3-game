using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Dich vu rung Camera su dung DOTween
public class CameraShakeService : Singleton<CameraShakeService>
{
    [SerializeField] private Camera targetCamera;

    private Tween currentShakeTween;
    private Vector3 originalLocalPos;
    private bool hasOriginalPos;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            originalLocalPos = targetCamera.transform.localPosition;
            hasOriginalPos = true;
        }
    }

    // Rung camera khi TNT don no
    public async UniTask ShakeTNT()
    {
        await ShakeAsync(0.25f, 0.15f, 18);
    }

    // Rung camera manh khi combo TNT+TNT hoac combo sieu lon
    public async UniTask ShakeMega()
    {
        await ShakeAsync(0.4f, 0.3f, 20);
    }

    // Thuc hien rung camera voi thong so tuy bien
    public async UniTask ShakeAsync(float duration, float strength, int vibrato)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
            originalLocalPos = targetCamera.transform.localPosition;
            hasOriginalPos = true;
        }

        // Reset vi tri neu dang co tween rung truoc do de tranh drift
        if (currentShakeTween != null && currentShakeTween.IsActive())
        {
            currentShakeTween.Kill();
            if (hasOriginalPos && targetCamera != null)
            {
                targetCamera.transform.localPosition = originalLocalPos;
            }
        }

        currentShakeTween = targetCamera.transform
            .DOShakePosition(duration, strength, vibrato, 90f, false, true)
            .OnKill(() =>
            {
                if (targetCamera != null && hasOriginalPos)
                {
                    targetCamera.transform.localPosition = originalLocalPos;
                }
            })
            .OnComplete(() =>
            {
                if (targetCamera != null && hasOriginalPos)
                {
                    targetCamera.transform.localPosition = originalLocalPos;
                }
            });

        await currentShakeTween.ToUniTask();
    }
}
