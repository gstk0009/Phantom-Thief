using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridPreviewRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridMap gridMap;
    [SerializeField] private Tilemap gridOverlayTilemap;
    [SerializeField] private Tilemap pathPreviewTilemap;

    [Header("Preview Tiles")]
    [SerializeField] private TileBase gridTile;
    [SerializeField] private TileBase pathTile;
    [SerializeField] private TileBase goalTile;

    [Header("Grid Visual")]
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float draggingAlpha = 0.30f;
    [SerializeField, Range(0f, 1f)] private float gridFadeDuration = 0.08f;

    [Header("Path Visual")]
    [SerializeField, Range(0f, 1f)] private float pathAlpha = 0.9f;

    private CancellationTokenSource gridFadeCts;

    private void Start()
    {
        BuildGrid();

        SetGridAlpha(0f);
        SetPathAlpha(pathAlpha);

        ClearPath();
    }

    private void OnDestroy()
    {
        CancelGridFade();
    }

    // Grid는 처음 한 번만 생성
    private void BuildGrid()
    {
        gridOverlayTilemap.ClearAllTiles();

        foreach (Vector3Int cell in gridMap.WalkableCells)
        {
            gridOverlayTilemap.SetTile(cell, gridTile);
        }
    }

    public void ShowSelectedGrid()
    {
        FadeGridAlphaAsync(selectedAlpha).Forget();
    }

    public void ShowDraggingGrid()
    {
        FadeGridAlphaAsync(draggingAlpha).Forget();
    }

    public void HideGrid()
    {
        FadeGridAlphaAsync(0f).Forget();
    }

    //Path
    public void RenderPath(IReadOnlyList<Vector3Int> path)
    {
        pathPreviewTilemap.ClearAllTiles();

        if (path == null || path.Count <= 1) return;


        // 중간 경로
        for (int i = 1; i < path.Count - 1; i++)
        {
            pathPreviewTilemap.SetTile(path[i], pathTile);
        }

        // 현재 목적지
        if (path.Count > 1)
        {
            pathPreviewTilemap.SetTile(path[path.Count - 1], goalTile);
        }
    }

    public void ClearPath()
    {
        pathPreviewTilemap.ClearAllTiles();
    }

    private void SetGridAlpha(float alpha)
    {
        Color color = gridOverlayTilemap.color;

        color.a = alpha;

        gridOverlayTilemap.color = color;
    }

    private void SetPathAlpha(float alpha)
    {
        Color color = pathPreviewTilemap.color;

        color.a = alpha;

        pathPreviewTilemap.color = color;
    }

    public void RefreshGridCell(Vector3Int cell)
    {
        if (gridMap.IsWalkable(cell))
        {
            gridOverlayTilemap.SetTile(cell, gridTile);
        }
        else
        {
            gridOverlayTilemap.SetTile(cell, null);
        }
    }

    public void RemovePathCell(Vector3Int cell)
    {
        pathPreviewTilemap.SetTile(cell, null);
    }

    private async UniTask FadeGridAlphaAsync(float targetAlpha)
    {
        CancelGridFade();

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        gridFadeCts = cts;

        float startAlpha = gridOverlayTilemap.color.a;

        if (gridFadeDuration <= 0f)
        {
            SetGridAlpha(targetAlpha);
            return;
        }

        float elapsed = 0f;

        try
        {
            while (elapsed < gridFadeDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / gridFadeDuration);
                SetGridAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));

                await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
            }

            SetGridAlpha(targetAlpha);
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            if (gridFadeCts == cts)
            {
                gridFadeCts = null;
                cts.Dispose();
            }
        }
    }

    private void CancelGridFade()
    {
        if (gridFadeCts == null) return;

        gridFadeCts.Cancel();
        gridFadeCts.Dispose();

        gridFadeCts = null;
    }
}