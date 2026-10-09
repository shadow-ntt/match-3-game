using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Utils;

// Xu ly logic dac thu cho combo giua LightBall va Booster khac:
// Bien doi tat ca gem cung mau thanh booster tuong ung va kich hoat day chuyen
public class LightBallComboHandler
{
    private readonly BoosterActivationManager _manager;

    public LightBallComboHandler(BoosterActivationManager manager)
    {
        _manager = manager;
    }

    private Board Board => _manager.Board;
    private BoosterEffectPlayer EffectPlayer => _manager.EffectPlayer;
    private PoolBlockBreakEffect PoolParticle => _manager.PoolParticle;

    // Xu ly combo LightBall + Booster khac:
    // 1. Chieu tia sang tu LightBall toi tat ca gem duoc chon (dominant color)
    // 2. Chuyen doi tat ca gem do thanh Booster dang ket hop (kem hieu ung pop)
    // 3. Kich hoat lan luot / day chuyen tat ca cac Booster vua tao
    public async UniTask HandleLightBallBoosterComboAsync(
        Vector3 centerPos,
        EnumItemBoard partnerType,
        List<Vector2Int> targetCells,
        HashSet<Vector2Int> visited)
    {
        if (targetCells == null || targetCells.Count == 0) return;

        // 1. Lay mau cua gem de to mau tia sang
        Color beamColor = GetFirstItemColor(targetCells);

        // 2. Ban cac tia sang dong loat tu LightBall toi tung gem muc tieu
        await EffectPlayer.PlayLightBallBeamsAsync(centerPos, targetCells, beamColor);

        // 3. Chuyen doi cac gem thanh Booster tuong ung
        var convertedPositions = new List<Vector2Int>();
        var popTasks = new List<UniTask>();

        for (int i = 0; i < targetCells.Count; i++)
        {
            Vector2Int pos = targetCells[i];
            if (!Board.IsInBounds(pos.x, pos.y)) continue;

            GameObject newBooster = ConvertGemToBooster(pos.x, pos.y, partnerType);
            if (newBooster != null)
            {
                convertedPositions.Add(pos);

                // Hieu ung pop xuat hien booster
                newBooster.transform.localScale = Vector3.zero;
                var popTween = newBooster.transform.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack);
                popTasks.Add(popTween.ToUniTask());
            }
        }

        if (popTasks.Count > 0)
        {
            await UniTask.WhenAll(popTasks);
        }

        // Delay nho de nguoi choi nhin ro cac booster moi duoc tao truoc khi no
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f));

        // 4. Kich hoat lan luot / day chuyen tat ca cac booster vua tao
        for (int i = 0; i < convertedPositions.Count; i++)
        {
            Vector2Int pos = convertedPositions[i];
            if (!Board.IsInBounds(pos.x, pos.y)) continue;
            if (visited.Contains(pos)) continue;

            GameObject obj = Board.MidGrid[pos.x, pos.y];
            if (obj == null) continue;

            if (obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsBoosterItem(item))
            {
                await _manager.ActivateBoosterInternalAsync(pos.x, pos.y, null, visited, 1);
                await _manager.DelayExplosionAsync(0.5f);
            }
        }
    }

    // Thay the 1 vien gem tai (x, y) thanh 1 vien Booster loai targetType
    public GameObject ConvertGemToBooster(int x, int y, EnumItemBoard targetType)
    {
        if (Board == null || Board.MidGrid == null || !Board.IsInBounds(x, y)) return null;

        GameObject existing = Board.MidGrid[x, y];
        if (existing == null) return null;

        // Chi convert gem thuong
        if (!existing.TryGetComponent<IBoardItem>(out var item) || !BoardItemUtils.IsValidNormalItem(item))
        {
            return null;
        }

        // Phat hieu ung hat no tai vien gem truoc khi bien doi
        if (PoolParticle != null)
        {
            PoolParticle.Play(existing.transform.position, item.ItemId);
        }

        Vector3 worldPos = GridUtils.GridToWorld(Board.Grid, x, y);
        Transform parent = Board.MidTilemap != null ? Board.MidTilemap.transform : Board.transform;

        // Tra gem cu ve pool
        if (Pooltem.Instance != null)
        {
            Pooltem.Instance.ReturnBoardItem(existing);
        }
        Board.MidGrid[x, y] = null;

        // Sinh booster moi tu pool
        GameObject newBooster = Pooltem.Instance != null ? Pooltem.Instance.SpawnBoardItem((int)targetType, worldPos, Quaternion.identity, parent) : null;
        if (newBooster == null) return null;

        Board.MidGrid[x, y] = newBooster;

        if (Board.BoardCellGrid != null &&
            Board.BoardCellGrid[x, y] != null &&
            Board.BoardCellGrid[x, y].TryGetComponent<BoardCell>(out var cell))
        {
            cell.State = EnumStateBoardCell.Occupied;
            cell.IsGettingFilled = false;
        }

        return newBooster;
    }

    // Lay mau cua vien ngoc hop le dau tien trong danh sach de to mau tia sang
    public Color GetFirstItemColor(List<Vector2Int> cells)
    {
        if (Board == null) return Color.yellow;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];
            if (Board.IsInBounds(pos.x, pos.y))
            {
                var obj = Board.MidGrid[pos.x, pos.y];
                if (obj != null && obj.TryGetComponent<IBoardItem>(out var item) && BoardItemUtils.IsValidNormalItem(item))
                {
                    return BoosterColorUtils.GetItemColor(item.ItemId);
                }
            }
        }
        return Color.yellow;
    }
}
