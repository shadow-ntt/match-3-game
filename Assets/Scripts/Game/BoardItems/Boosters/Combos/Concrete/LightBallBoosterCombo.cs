using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly combo LightBall + Booster khac:
// Bien doi tat ca gem cung mau (dominant) thanh booster tuong ung va kich hoat day chuyen
public class LightBallBoosterCombo : BoosterCombo
{
    private EnumItemBoard _dominantColor = EnumItemBoard.Blank;

    public LightBallBoosterCombo()
    {
        TypeA = EnumItemBoard.LightBall;
        TypeB = EnumItemBoard.Blank; // Blank dai dien cho bat ky booster nao khac
    }

    public override bool Matches(EnumItemBoard a, EnumItemBoard b)
    {
        return (a == EnumItemBoard.LightBall || b == EnumItemBoard.LightBall) && (a != b);
    }

    public override List<Vector2Int> GetAffectedCells(Board board, int cx, int cy, EnumItemBoard a, EnumItemBoard b)
    {
        var result = new HashSet<Vector2Int>();
        if (board == null) return new List<Vector2Int>();

        _dominantColor = BoosterComboGridUtils.PickDominantColor(board);
        if (_dominantColor != EnumItemBoard.Blank)
        {
            BoosterComboGridUtils.AddAllGemsOfColor(board, _dominantColor, result);
        }

        return new List<Vector2Int>(result);
    }

    public override async UniTask PlayMergeAnimationAsync(GameObject objA, GameObject objB, Vector3 centerPos)
    {
        if (objA == null || objB == null) return;

        bool isALightBall = objA.GetComponent<LightBallItem>() != null;
        GameObject objLB = isALightBall ? objA : objB;
        GameObject objOther = isALightBall ? objB : objA;

        var moveOther = objOther.transform.DOMove(objLB.transform.position, 0.18f);
        var scaleOther = objOther.transform.DOScale(0.2f, 0.18f);
        var scaleLB = objLB.transform.DOScale(1.5f, 0.18f);
        var rotLB = objLB.transform.DORotate(new Vector3(0, 0, 360f), 0.22f, RotateMode.FastBeyond360);
        await UniTask.WhenAll(moveOther.ToUniTask(), scaleOther.ToUniTask(), scaleLB.ToUniTask(), rotLB.ToUniTask());

        var flashLB = objLB.transform.DOScale(1.8f, 0.08f).SetLoops(2, LoopType.Yoyo);
        await flashLB.ToUniTask();
    }

    public override async UniTask ExecuteEffectAsync(BoosterComboContext context)
    {
        if (context == null || context.Board == null || context.Manager == null) return;

        var board = context.Board;
        var manager = context.Manager;
        var effectPlayer = context.EffectPlayer;
        var targetCells = context.AffectedCells;
        var visited = context.Visited;
        Vector3 centerPos = context.CenterWorldPos;

        if (targetCells == null || targetCells.Count == 0) return;

        EnumItemBoard partnerType = (context.TypeA == EnumItemBoard.LightBall) ? context.TypeB : context.TypeA;

        Color beamColor = BoosterColorUtils.GetItemColor(_dominantColor);

        if (effectPlayer != null)
        {
            await effectPlayer.PlayLightBallBeamsAsync(centerPos, targetCells, beamColor);
        }

        var convertedPositions = new List<Vector2Int>();
        var popTasks = new List<UniTask>();

        for (int i = 0; i < targetCells.Count; i++)
        {
            Vector2Int pos = targetCells[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;

            GameObject newBooster = ConvertGemToBooster(board, manager, pos.x, pos.y, partnerType);
            if (newBooster != null)
            {
                convertedPositions.Add(pos);

                newBooster.transform.localScale = Vector3.zero;
                var popTween = newBooster.transform.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack);
                popTasks.Add(popTween.ToUniTask());
            }
        }

        if (popTasks.Count > 0)
        {
            await UniTask.WhenAll(popTasks);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f));

        var activationTasks = new List<UniTask>();

        for (int i = 0; i < convertedPositions.Count; i++)
        {
            Vector2Int pos = convertedPositions[i];
            if (!board.IsInBounds(pos.x, pos.y)) continue;
            if (visited != null && visited.Contains(pos)) continue;

            GameObject obj = board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsBoosterItem(item))
            {

                activationTasks.Add(manager.ActivateBoosterInternalAsync(pos.x, pos.y, null, visited, 1));
            }
        }

        if (activationTasks.Count > 0)
        {
            await UniTask.WhenAll(activationTasks);
        }
    }

    private GameObject ConvertGemToBooster(Board board, BoosterActivationManager manager, int x, int y, EnumItemBoard targetType)
    {
        if (board == null || board.MidGrid == null || !board.IsInBounds(x, y)) return null;

        GameObject existing = board.MidGrid[x, y];
        if (existing == null) return null;

        if (!existing.TryGetComponent<IBoardItem>(out var item) || !BoardItemUtils.IsValidNormalItem(item))
        {
            return null;
        }

        if (manager != null && manager.PoolParticle != null)
        {
            manager.PoolParticle.Play(existing.transform.position, item.ItemId);
        }

        Vector3 worldPos = GridUtils.GridToWorld(board.Grid, x, y);
        Transform parent = board.MidTilemap != null ? board.MidTilemap.transform : board.transform;

        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(existing);
        }
        board.MidGrid[x, y] = null;

        GameObject newBooster = Pooltem.Instance != null ? Pooltem.Instance.SpawnBoardItem((int)targetType, worldPos, Quaternion.identity, parent) : null;
        if (newBooster == null) return null;

        board.MidGrid[x, y] = newBooster;

        if (board.BoardCellGrid != null &&
            board.BoardCellGrid[x, y] != null &&
            board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Occupied;
            cell.IsGettingFilled = false;
        }

        return newBooster;
    }
}
