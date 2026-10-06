using System.Collections.Generic;
using UnityEngine;

// Du lieu chua thong tin cua mot cum Match tren ban co
[System.Serializable]
public class MatchData
{
    // Id cua loai item duoc match (EnumItemBoard)
    public int matchID;

    // Loai match (Normal, Rocket, TNT, Missile, LightBall)
    public MatchType MatchType;

    // Tam tao booster (vi tri sinh item booster)
    public Vector2Int centerCell;

    // Danh sach toa do cac o tham gia match
    public List<Vector2Int> Matches = new List<Vector2Int>();

    public MatchData()
    {
        Matches = new List<Vector2Int>();
    }

    public MatchData(int id, MatchType type, List<Vector2Int> cells, Vector2Int center = default)
    {
        matchID = id;
        MatchType = type;
        Matches = cells ?? new List<Vector2Int>();
        centerCell = center != default ? center : (Matches.Count > 0 ? Matches[0] : Vector2Int.zero);
    }

    // Tam tao booster
    public Vector2Int CenterCell => centerCell;
}
