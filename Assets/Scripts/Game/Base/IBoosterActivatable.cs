using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Giao dien khai bao kha nang kich hoat hieu ung Booster
public interface IBoosterActivatable
{
    // Kiem tra booster co the kich hoat hay khong (vi du: LightBall can swapTarget hop le o depth 0)
    bool CanActivate(int depth, IBoardItem swapTarget = null);

    // Lay danh sach cac o bi anh huong boi booster
    List<Vector2Int> GetAffectedCells(Board board, int x, int y, IBoardItem swapTarget = null);

    // Phat animation kich hoat cua chinh booster truoc khi no
    UniTask PlayActivationAnimationAsync();

    // Thuc thi hieu ung phat no / phong dan dac thu cua loai booster
    UniTask ExecuteActivationEffectAsync(BoosterActivationContext context);
}

