using System.Collections.Generic;
using UnityEngine;

public class MovementPlan
{
    private readonly List<Vector3Int> cells;

    public IReadOnlyList<Vector3Int> Cells => cells;

    public int TurnCost => Mathf.Max(0, cells.Count - 1);

    public MovementPlan(IReadOnlyList<Vector3Int> sourceCells)
    {
        cells = new List<Vector3Int>(sourceCells.Count);

        for (int i = 0; i < sourceCells.Count; i++)
        {
            cells.Add(sourceCells[i]);
        }
    }
}