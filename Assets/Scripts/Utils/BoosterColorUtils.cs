using UnityEngine;

// Tien ich mau sac cho cac vien ngoc va booster
public static class BoosterColorUtils
{
    // Tra ve mau sac dai dien cho tung loai ngoc
    public static Color GetItemColor(EnumItemBoard itemId)
    {
        switch (itemId)
        {
            case EnumItemBoard.Red: return Color.red;
            case EnumItemBoard.Blue: return new Color(0.2f, 0.6f, 1f);
            case EnumItemBoard.Green: return Color.green;
            case EnumItemBoard.Yellow: return Color.yellow;
            case EnumItemBoard.Purple: return new Color(0.7f, 0.2f, 1f);
            case EnumItemBoard.Orange: return new Color(1f, 0.5f, 0f);
            case EnumItemBoard.Pink: return new Color(1f, 0.4f, 0.7f);
            default: return Color.yellow;
        }
    }
}
