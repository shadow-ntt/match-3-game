using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Dau dan ten lua dan duong bay theo duong cong den muc tieu (Missile Projectile)
public class MissileProjectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private float curveHeight = 1.2f;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
    }

    // Bay theo duong cong CatmullRom den toa do muc tieu
    public async UniTask FlyToTargetAsync(Vector3 startPos, Vector3 targetPos, float duration = 0.45f)
    {
        transform.position = startPos;

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        // Tao diem uon cong len tren cao giup ten lua co quy dao bay tu nhien

        float randomOffsetX = UnityEngine.Random.Range(-0.5f, 0.5f);
        Vector3 midPoint = (startPos + targetPos) * 0.5f + Vector3.up * curveHeight + Vector3.right * randomOffsetX;

        Vector3[] waypoints = new Vector3[] { startPos, midPoint, targetPos };
        Vector3 lastPos = startPos;

        var tween = transform.DOPath(waypoints, duration, PathType.CatmullRom)
            .SetEase(Ease.InQuad)
            .OnUpdate(() =>
            {
                Vector3 currentPos = transform.position;
                Vector3 moveDir = currentPos - lastPos;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                    lastPos = currentPos;
                }
            });

        await tween.ToUniTask();

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
}
