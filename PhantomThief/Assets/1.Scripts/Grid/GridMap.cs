using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMap : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap floorTileMap;

    [Header("Blocking Tilemaps")]
    [SerializeField] private Tilemap[] blockingTileMaps;

    private readonly HashSet<Vector3Int> walkableCells = new();
    public IReadOnlyCollection<Vector3Int> WalkableCells => walkableCells;

    private void Awake()
    {
        BuildWalkableCache();
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return grid.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return grid.GetCellCenterWorld(cell);
    }

    public bool IsWalkable(Vector3Int cell)
    {
        return walkableCells.Contains(cell);
    }

    // 드래그를 빠르게 했을 때 입력 위치가 중간 셀을 건너뛰고 2칸 이상 떨어진 셀로 잡히는걸 그대로 경로에 넣지 않게 막는 1차 방어
    public bool IsAdjacent(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        return dx <= 1 && dy <= 1 && (dx + dy > 0);
    }

    public bool CanMove(Vector3Int from, Vector3Int to)
    {
        if (!IsAdjacent(from, to)) return false;
        if (!IsWalkable(to)) return false;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool diagonal = dx != 0 && dy != 0;

        if (!diagonal) return true;

        // Corner Cutting 방지
        Vector3Int horizontal = new Vector3Int(from.x + dx, from.y, from.z);
        Vector3Int vertical = new Vector3Int(from.x, from.y + dy, from.z);

        return IsWalkable(horizontal) && IsWalkable(vertical);
    }

    private void BuildWalkableCache()
    {
        walkableCells.Clear();

        foreach (Vector3Int cell in floorTileMap.cellBounds.allPositionsWithin)
        {
            if (CalculateWalkable(cell))
            {
                walkableCells.Add(cell);
            }
        }
    }

    private bool CalculateWalkable(Vector3Int cell)
    {
        if (!floorTileMap.HasTile(cell)) return false;

        foreach (Tilemap blockingTilemap in blockingTileMaps)
        {
            if (blockingTilemap == null) continue;

            if (blockingTilemap.HasTile(cell)) return false;
        }

        return true;
    }

    public void RefreshWalkableCell(Vector3Int cell)
    {
        if (CalculateWalkable(cell))
        {
            walkableCells.Add(cell);
        }
        else
        {
            walkableCells.Remove(cell);
        }
    }

    public Vector2 WorldDeltaToCellDelta(Vector2 worldDelta)
    {
        Vector3 localDelta = grid.transform.InverseTransformVector(new Vector3(worldDelta.x, worldDelta.y, 0f));

        return new Vector2(localDelta.x / grid.cellSize.x, localDelta.y / grid.cellSize.y);
    }
}
