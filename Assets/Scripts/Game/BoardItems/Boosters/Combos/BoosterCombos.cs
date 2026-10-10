using System.Collections.Generic;
using Utils;

// Khoi tao va luu tru tat ca cac loai Combo Booster bang Dictionary, ho tro tra cuu O(1)
public class BoosterCombos
{
    private readonly Dictionary<(EnumItemBoard, EnumItemBoard), BoosterCombo> _comboDict;
    private readonly BoosterCombo _fallbackCombo;

    public BoosterCombos()
    {
        _fallbackCombo = new DefaultFallbackCombo();
        _comboDict = new Dictionary<(EnumItemBoard, EnumItemBoard), BoosterCombo>();

        // 1. Rocket + Rocket
        Register(EnumItemBoard.VerticalRocket, EnumItemBoard.VerticalRocket, new VerticalRocketCombo());
        Register(EnumItemBoard.HorizontalRocket, EnumItemBoard.HorizontalRocket, new HorizontalRocketCombo());
        Register(EnumItemBoard.HorizontalRocket, EnumItemBoard.VerticalRocket, new RocketCrossCombo());

        // 2. TNT Combos
        Register(EnumItemBoard.TNT, EnumItemBoard.TNT, new TNTTNTCombo());
        Register(EnumItemBoard.HorizontalRocket, EnumItemBoard.TNT, new TNTHorizontalRocketCombo());
        Register(EnumItemBoard.VerticalRocket, EnumItemBoard.TNT, new TNTVerticalRocketCombo());

        // 3. Missile Combos
        Register(EnumItemBoard.Missile, EnumItemBoard.Missile, new MissileMissileCombo());
        Register(EnumItemBoard.TNT, EnumItemBoard.Missile, new MissileTNTCombo());
        Register(EnumItemBoard.HorizontalRocket, EnumItemBoard.Missile, new MissileHorizontalRocketCombo());
        Register(EnumItemBoard.VerticalRocket, EnumItemBoard.Missile, new MissileVerticalRocketCombo());

        // 4. LightBall Combos
        Register(EnumItemBoard.LightBall, EnumItemBoard.LightBall, new LightBallLightBallCombo());

        var lightBallBoosterCombo = new LightBallBoosterCombo();
        Register(EnumItemBoard.HorizontalRocket, EnumItemBoard.LightBall, lightBallBoosterCombo);
        Register(EnumItemBoard.VerticalRocket, EnumItemBoard.LightBall, lightBallBoosterCombo);
        Register(EnumItemBoard.TNT, EnumItemBoard.LightBall, lightBallBoosterCombo);
        Register(EnumItemBoard.Missile, EnumItemBoard.LightBall, lightBallBoosterCombo);
    }

    private void Register(EnumItemBoard a, EnumItemBoard b, BoosterCombo combo)
    {
        var key = a <= b ? (a, b) : (b, a);
        _comboDict[key] = combo;
    }

    // Tra ve combo phu hop theo cap booster (typeA, typeB) voi do phuc tap O(1)
    public BoosterCombo GetCombo(EnumItemBoard typeA, EnumItemBoard typeB)
    {
        var key = typeA <= typeB ? (typeA, typeB) : (typeB, typeA);
        if (_comboDict.TryGetValue(key, out var combo))
        {
            return combo;
        }

        // Truong hop booster moi tuong lai chua dang ky rieng ghep voi LightBall
        if ((typeA == EnumItemBoard.LightBall || typeB == EnumItemBoard.LightBall) && typeA != typeB)
        {
            if (_comboDict.TryGetValue((EnumItemBoard.HorizontalRocket, EnumItemBoard.LightBall), out var lbCombo))
            {
                return lbCombo;
            }
        }

        return _fallbackCombo;
    }
}
