using System;
using UnityEngine;

public static class DragDirectionResolver
{
    public static Vector3Int Resolve(Vector3Int currentCell, Vector3Int targetCell, Vector2 pointerCellDelta, float diagonalRatio)
    {
        int dx = targetCell.x - currentCell.x;
        int dy = targetCell.y - currentCell.y;

        if (dx == 0 && dy == 0) return Vector3Int.zero;

        int xDirection = dx > 0f ? 1 : dx < 0f ? -1 : 0;
        int yDirection = dy > 0f ? 1 : dy < 0f ? -1 : 0;

        if (dy == 0) return new Vector3Int(xDirection, 0, 0);
        if (dx == 0) return new Vector3Int(0, yDirection, 0);

        float absX = Mathf.Abs(pointerCellDelta.x);
        float absY = Mathf.Abs(pointerCellDelta.y);

        float maxValue = Mathf.Max(absX, absY);

        if (maxValue <= Mathf.Epsilon) return Vector3Int.zero;

        float minValue = MathF.Min(absX, absY);
        float ratio = minValue / maxValue;

        if (ratio >= diagonalRatio) return new Vector3Int(xDirection, yDirection, 0);

        if (absX >= absY) return new Vector3Int(xDirection, 0, 0);

        return new Vector3Int(0, yDirection, 0);
    }
}