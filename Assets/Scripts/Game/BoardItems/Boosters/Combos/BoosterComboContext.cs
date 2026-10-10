using System.Collections.Generic;
using UnityEngine;
using Utils;

// Dong goi toan bo thong tin ngu canh khi kich hoat mot combo booster
public class BoosterComboContext
{
    public Board Board { get; }
    public BoosterActivationManager Manager { get; }
    public BoosterEffectPlayer EffectPlayer => Manager?.EffectPlayer;

    public int CenterX { get; }
    public int CenterY { get; }
    public int XA { get; }
    public int YA { get; }
    public int XB { get; }
    public int YB { get; }

    public EnumItemBoard TypeA { get; }
    public EnumItemBoard TypeB { get; }

    public List<Vector2Int> AffectedCells { get; }
    public List<Vector2Int> ChainBoosters { get; }
    public HashSet<Vector2Int> Visited { get; }

    public Vector3 CenterWorldPos => Board != null && Board.Grid != null
        ? GridUtils.GridToWorld(Board.Grid, CenterX, CenterY)
        : Vector3.zero;

    public BoosterComboContext(
        Board board,
        BoosterActivationManager manager,
        int cx, int cy,
        int xA, int yA,
        int xB, int yB,
        EnumItemBoard typeA,
        EnumItemBoard typeB,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters,
        HashSet<Vector2Int> visited)
    {
        Board = board;
        Manager = manager;
        CenterX = cx;
        CenterY = cy;
        XA = xA;
        YA = yA;
        XB = xB;
        YB = yB;
        TypeA = typeA;
        TypeB = typeB;
        AffectedCells = affectedCells ?? new List<Vector2Int>();
        ChainBoosters = chainBoosters ?? new List<Vector2Int>();
        Visited = visited ?? new HashSet<Vector2Int>();
    }
}
