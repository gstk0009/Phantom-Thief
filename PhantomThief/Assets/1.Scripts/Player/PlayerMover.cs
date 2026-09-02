using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridMap gridMap;
    [SerializeField] private PlayerFacingController facing;

    [Header("Visual")]
    [SerializeField] private float moveDurationPerCell = 0.1f;

    public bool IsMoving { get; private set; }
    public event Action<Vector3Int> CellReached;

    public void Execute(MovementPlan plan)
    {
        ExecuteAsync(plan).Forget();
    }

    public async UniTask ExecuteAsync(MovementPlan plan)
    {
        if (IsMoving) return;
        if (plan == null) return;
        if (plan.Cells.Count <= 1) return;

        IsMoving = true;

        if (facing != null) facing.SetMoving(true);

        try
        {
            for (int i = 1; i < plan.Cells.Count; i++)
            {
                Vector3Int previousCell = plan.Cells[i - 1];
                Vector3Int nextCell = plan.Cells[i];

                Vector3Int direction = nextCell - previousCell;

                if (facing != null) facing.SetDirection(direction);

                await MoveToCellAsync(nextCell);
                CellReached?.Invoke(nextCell);
            }
        }
        finally
        {
            if (facing != null) facing.SetMoving(false);
            IsMoving = false;
        }
    }

    private async UniTask MoveToCellAsync(Vector3Int targetCell)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = gridMap.CellToWorld(targetCell);

        if (moveDurationPerCell <= 0f)
        {
            transform.position = targetPosition;
            return;
        }

        float timer = 0f;

        while (timer < moveDurationPerCell)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDurationPerCell);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        transform.position = targetPosition;
    }
}