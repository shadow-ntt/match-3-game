using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Booster TNT: no vung 3x3 xung quanh tam booster
public class TNTItem : BoosterItem
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }


    public override List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null)
    {
        var cells = new List<Vector2Int>();
        if (board == null || board.MidGrid == null) return cells;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (board.IsInBounds(nx, ny) && (board.MidGrid[nx, ny] != null || (board.OverlayGrid != null && board.OverlayGrid[nx, ny] != null)))
                {
                    cells.Add(new Vector2Int(nx, ny));
                }
            }
        }

        return cells;
    }

    // Phat animation nhap nhay do-trang va rung manh truoc khi phat no 3x3
    public override async UniTask PlayActivationAnimationAsync()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, 0.07f).SetLoops(4, LoopType.Yoyo);
        }

        await transform.DOShakePosition(0.28f, 0.12f, 15, 90f, false, true).ToUniTask();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    public override async UniTask ExecuteActivationEffectAsync(BoosterActivationContext context)
    {
        // Rung camera khi TNT don no
        var shake = CameraShakeService.Instance;
        shake?.ShakeTNT().Forget();

        if (context?.EffectPlayer != null)
        {
            await context.EffectPlayer.PlayWaveExplosionAsync(context.X, context.Y, 1, context.AffectedCells, context.ChainBoosters);
        }

        if (context?.Manager != null)
        {
            await context.Manager.DelayExplosionAsync();
        }
    }
}

