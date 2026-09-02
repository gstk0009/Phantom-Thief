using System.Collections.Generic;
using UnityEngine;

public class DragPathBuilder
{
    private readonly GridMap gridMap;
    private readonly List<Vector3Int> path = new List<Vector3Int>();
    public IReadOnlyList<Vector3Int> Path => path;
    public int TurnCost => Mathf.Max(0, path.Count - 1);
    public bool IsAtStart => path.Count <= 1;
    public Vector3Int CurrentCell => path[^1];
    public DragPathBuilder(GridMap gridMap)
    {
        this.gridMap = gridMap;
    }
    public void Begin(Vector3Int startCell)
    {
        path.Clear();
        path.Add(startCell);
    }

    public bool TryEnterCell(Vector3Int cell)
    {
        if (path.Count == 0) return false;

        Vector3Int currentCell = path[path.Count - 1];

        if (cell == currentCell) return false;

        if (!gridMap.IsAdjacent(currentCell, cell)) return false;

        int existingIndex = path.IndexOf(cell);
        if (existingIndex >= 0)
        {
            int removeCount = path.Count - existingIndex - 1;

            if (removeCount > 0)
            {
                path.RemoveRange(existingIndex + 1, removeCount);
            }

            return true;
        }

        if (!gridMap.CanMove(currentCell, cell)) return false;

        path.Add(cell);

        return true;
    }

    public void Clear()
    {
        path.Clear();
    }
}