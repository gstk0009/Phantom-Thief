using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDragController : MonoBehaviour
{
    private enum InputState
    {
        Idle,
        Selected,
        PressingPlayer,
        PressingGrid,
        Dragging,
        Executing
    }
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GridMap gridMap;

    [SerializeField]
    private GridPreviewRenderer previewRenderer;
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] Collider2D playerTouchCollider;

    [Header("Tap")]
    [SerializeField] private float tapMoveTolerance = 12f;
    [SerializeField] private float tapPathPreviewDuration = 0.20f;

    [Header("Drag")]
    [SerializeField] private float longPressDuration = 0.12f;
    [SerializeField, Range(0.1f, 0.8f)] private float diagonalRatio = 0.65f;

    private PlayerInputActions inputActions;
    private DragPathBuilder pathBuilder;
    private GridPathService pathService;
    private CancellationTokenSource longPressCts;
    private InputState state = InputState.Idle;
    private Vector2 pressScreenPosition;
    private Vector2 currentScreenPosition;
    private Vector3Int pressedGridCell;
    private bool playerWasSelectedOnPress;
    private bool dragStartedFromSelected;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        pathBuilder = new DragPathBuilder(gridMap);
        pathService = new GridPathService(gridMap);
    }

    private void OnEnable()
    {
        inputActions.GamePlay.Point.performed += OnPointerFormed;
        inputActions.GamePlay.Press.started += OnPressStarted;
        inputActions.GamePlay.Press.canceled += OnPressCanceled;
        inputActions.GamePlay.Enable();
        playerMover.CellReached += OnPlayerCellReached;
    }

    private void OnDisable()
    {
        inputActions.GamePlay.Point.performed -= OnPointerFormed;
        inputActions.GamePlay.Press.started -= OnPressStarted;
        inputActions.GamePlay.Press.canceled -= OnPressCanceled;
        inputActions.GamePlay.Disable();
        CancelLongPress();
        playerMover.CellReached -= OnPlayerCellReached;
    }

    private void OnDestroy()
    {
        CancelLongPress();
        inputActions?.Dispose();
    }

    // Pointer 위치 변경
    private void OnPointerFormed(InputAction.CallbackContext context)
    {
        currentScreenPosition = context.ReadValue<Vector2>();

        if (state == InputState.Dragging) UpdateDrag();
    }

    // 클릭 / 터치 시작
    private void OnPressStarted(InputAction.CallbackContext context)
    {
        if (playerMover.IsMoving) return;
        if (state == InputState.Executing) return;

        pressScreenPosition = inputActions.GamePlay.Point.ReadValue<Vector2>();
        currentScreenPosition = pressScreenPosition;
        Vector2 worldPosition = ScreenToWorld(pressScreenPosition);

        if (playerTouchCollider.OverlapPoint(worldPosition))
        {
            playerWasSelectedOnPress =
            state == InputState.Selected;

            state = InputState.PressingPlayer;

            StartLongPressAsync().Forget();

            return;
        }

        if (state != InputState.Selected) return;

        pressedGridCell = gridMap.WorldToCell(worldPosition);
        state = InputState.PressingGrid;
    }
    // 클릭 / 터치 종료
    private void OnPressCanceled(InputAction.CallbackContext context)
    {
        CancelLongPress();

        currentScreenPosition = inputActions.GamePlay.Point.ReadValue<Vector2>();

        switch (state)
        {
            case InputState.PressingPlayer:
                HandlePlayerTap();
                break;
            case InputState.PressingGrid:
                TryExecuteTapMove();
                break;
            case InputState.Dragging:
                UpdateDrag();
                EndDrag();
                break;
        }
    }

    // Player 짧은 Tap
    private void HandlePlayerTap()
    {
        if (playerWasSelectedOnPress)
        {
            DeselectPlayer();
            return;
        }
        SelectPlayer();
    }

    private void SelectPlayer()
    {
        state = InputState.Selected;
        previewRenderer.ClearPath();
        previewRenderer.ShowSelectedGrid();
    }

    private void DeselectPlayer()
    {
        state = InputState.Idle;
        previewRenderer.ClearPath();
        previewRenderer.HideGrid();
    }

    private async UniTaskVoid StartLongPressAsync()
    {
        CancelLongPress();

        longPressCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(longPressDuration), cancellationToken: longPressCts.Token);

            if (state != InputState.PressingPlayer) return;

            BeginDrag();
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void BeginDrag()
    {
        Vector3Int playerCell = gridMap.WorldToCell(playerMover.transform.position);
        pathBuilder.Begin(playerCell);
        dragStartedFromSelected = playerWasSelectedOnPress;

        state = InputState.Dragging;
        previewRenderer.ShowDraggingGrid();
        previewRenderer.ClearPath();

        UpdateDrag();
    }

    // Drag 중 Path 생성
    private void UpdateDrag()
    {
        Vector2 pointerWorld = ScreenToWorld(currentScreenPosition);
        Vector3Int targetCell = gridMap.WorldToCell(pointerWorld);

        const int maxStepPerUpdate = 12;
        bool pathChanged = false;

        for (int i = 0; i < maxStepPerUpdate; i++)
        {
            Vector3Int currentCell = pathBuilder.CurrentCell;

            if (currentCell == targetCell) break;

            Vector2 currentCellCenter = gridMap.CellToWorld(currentCell);
            Vector2 worldDelta = pointerWorld - currentCellCenter;
            Vector2 cellDelta = gridMap.WorldDeltaToCellDelta(worldDelta);

            Vector3Int direction = DragDirectionResolver.Resolve(currentCell, targetCell, cellDelta, diagonalRatio);

            if (direction == Vector3Int.zero) break;

            Vector3Int nextCell = currentCell + direction;
            bool changed = pathBuilder.TryEnterCell(nextCell);
            if (!changed) break;

            pathChanged = true;
        }

        if (!pathChanged) return;

        previewRenderer.RenderPath(pathBuilder.Path);
    }

    // Drag 종료
    private void EndDrag()
    {
        if (pathBuilder.IsAtStart)
        {
            pathBuilder.Clear();

            previewRenderer.ClearPath();

            if (dragStartedFromSelected) SelectPlayer();
            else DeselectPlayer();

            return;
        }

        MovementPlan plan = new MovementPlan(pathBuilder.Path);

        pathBuilder.Clear();

        StartMoveAsync(plan, 0f).Forget();
    }

    private void TryExecuteTapMove()
    {
        float pointerDistance = Vector2.Distance(pressScreenPosition, currentScreenPosition);
        if (pointerDistance > tapMoveTolerance)
        {
            state = InputState.Selected;
            return;
        }

        if (!gridMap.IsWalkable(pressedGridCell))
        {
            state = InputState.Selected;
            return;
        }

        Vector3Int playerCell = gridMap.WorldToCell(playerMover.transform.position);
        List<Vector3Int> path = pathService.FindPath(playerCell, pressedGridCell);

        if (path == null || path.Count <= 1)
        {
            state = InputState.Selected;
            return;
        }

        previewRenderer.RenderPath(path);

        MovementPlan plan = new MovementPlan(path);

        StartMoveAsync(plan, tapPathPreviewDuration).Forget();
    }

    private async UniTask StartMoveAsync(MovementPlan plan, float previewDuration)
    {
        state = InputState.Executing;
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        try
        {
            if (previewDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(previewDuration), cancellationToken: token);
            }

            previewRenderer.HideGrid();

            await playerMover.ExecuteAsync(plan);

            previewRenderer.ClearPath();

            state = InputState.Idle;
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void CancelLongPress()
    {
        if (longPressCts == null) return;

        longPressCts.Cancel();
        longPressCts.Dispose();
        longPressCts = null;
    }

    private Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));

        return worldPosition;
    }

    private void OnPlayerCellReached(Vector3Int cell)
    {
        previewRenderer.RemovePathCell(cell);
    }
}