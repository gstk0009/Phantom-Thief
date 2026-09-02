using System.Collections.Generic;
using UnityEngine;

public static class GridLineRasterizer
{
    public static IEnumerable<Vector3Int> GetCells(Vector3Int from, Vector3Int to)
    {
        int x0 = from.x;
        int y0 = from.y;

        int x1 = to.x;
        int y1 = to.y;

        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;

        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;

        int error = dx + dy;

        while (true)
        {
            yield return new Vector3Int(x0, y0, from.z);

            if (x0 == x1 && y0 == y1) break;

            int error2 = 2 * error;

            if (error2 >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (error2 <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }
}
