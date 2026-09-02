using System.Collections.Generic;
using UnityEngine;

public class GridPathService
{
    private static readonly Vector3Int[] Directions =
    {
        new Vector3Int (1,0,0),
        new Vector3Int (-1,0,0),
        new Vector3Int (0,1,0),
        new Vector3Int (0,-1,0),

        new Vector3Int (1,1,0),
        new Vector3Int (1,-1,0),
        new Vector3Int (-1,1,0),
        new Vector3Int (-1,-1,0)
    };

    private readonly GridMap gridMap;

    public GridPathService(GridMap gridMap)
    {
        this.gridMap = gridMap;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        if (start == goal) return new List<Vector3Int> { start };
        if (!gridMap.IsWalkable(goal)) return null;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> parent = new Dictionary<Vector3Int, Vector3Int>();

        queue.Enqueue(start);
        parent[start] = start;

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            foreach (Vector3Int direction in Directions)
            {
                Vector3Int next = current + direction;
                if (parent.ContainsKey(next)) continue;
                if (!gridMap.CanMove(current, next)) continue;

                parent[next] = current;

                if (next == goal) return BuildPath(parent, start, goal);
                queue.Enqueue(next);
            }
        }

        return null;
    }

    public List<Vector3Int> BuildPath(Dictionary<Vector3Int, Vector3Int> parent, Vector3Int start, Vector3Int goal)
    {
        List<Vector3Int> result = new List<Vector3Int>();

        Vector3Int current = goal;

        while (current != start)
        {
            result.Add(current);

            current = parent[current];
        }

        result.Add(start);
        result.Reverse();
        return result;
    }
}