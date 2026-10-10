using System.Collections.Generic;
using UnityEngine;

// Dong goi toan bo thong tin ngu canh khi kich hoat mot booster don le
public class BoosterActivationContext
{
    public Board Board { get; }
    public BoosterActivationManager Manager { get; }
    public BoosterEffectPlayer EffectPlayer => Manager?.EffectPlayer;

    public int X { get; }
    public int Y { get; }
    public Vector3 WorldPos => Board != null && Board.Grid != null
        ? Utils.GridUtils.GridToWorld(Board.Grid, X, Y)
        : Vector3.zero;

    public List<Vector2Int> AffectedCells { get; }
    public List<Vector2Int> ChainBoosters { get; }
    public HashSet<Vector2Int> Visited { get; }
    public IBoardItem SwapTarget { get; }
    public int Depth { get; }

    public BoosterActivationContext(
        Board board,
        BoosterActivationManager manager,
        int x,
        int y,
        List<Vector2Int> affectedCells,
        List<Vector2Int> chainBoosters,
        HashSet<Vector2Int> visited,
        IBoardItem swapTarget = null,
        int depth = 0)
    {
        Board = board;
        Manager = manager;
        X = x;
        Y = y;
        AffectedCells = affectedCells ?? new List<Vector2Int>();
        ChainBoosters = chainBoosters ?? new List<Vector2Int>();
        Visited = visited ?? new HashSet<Vector2Int>();
        SwapTarget = swapTarget;
        Depth = depth;
    }
}
