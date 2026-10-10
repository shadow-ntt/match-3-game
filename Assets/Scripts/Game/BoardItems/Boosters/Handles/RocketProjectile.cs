using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Dau dan ten lua bay qua cac o tren ban co (Rocket Projectile)
public class RocketProjectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
    }

    // Bay lan luot qua danh sach cac o va goi callback onPassCell tai moi o
    public async UniTask FlyAlongLineAsync(
        Vector3 startPos,
        Vector3 exitPos,
        List<Vector2Int> pathCells,
        Grid grid,
        Action<Vector2Int> onPassCell,
        float cellDuration = 0.08f)
    {
        transform.position = startPos;

        // Tinh goc xoay theo huong bay
        Vector3 dir = (exitPos - startPos).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        // Bay qua tung o va kich hoat pha huy o do
        if (pathCells != null)
        {
            for (int i = 0; i < pathCells.Count; i++)
            {
                Vector2Int cell = pathCells[i];
                Vector3 targetWorld = GridUtils.GridToWorld(grid, cell.x, cell.y);
                await transform.DOMove(targetWorld, cellDuration).SetEase(Ease.Linear).ToUniTask();
                onPassCell?.Invoke(cell);
            }
        }

        // Bay tiep ra ngoai mep ban co truoc khi thu hoi
        await transform.DOMove(exitPos, cellDuration).SetEase(Ease.Linear).ToUniTask();

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
}
