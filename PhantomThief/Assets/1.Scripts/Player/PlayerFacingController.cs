using UnityEngine;

public class PlayerFacingController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private static readonly int FaceXHash = Animator.StringToHash("FaceX");
    private static readonly int FaceYHash = Animator.StringToHash("FaceY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    public void SetDirection(Vector3Int delta)
    {
        if (animator == null) return;

        float x = 0f;
        float y = 0f;

        if (delta.x > 0) { x = 1f; }
        else if (delta.x < 0) { x = -1f; }

        if (delta.y > 0) { y = 1f; }
        else if (delta.y < 0) { y = -1f; }

        animator.SetFloat(FaceXHash, x);
        animator.SetFloat(FaceYHash, y);
    }

    public void SetMoving(bool moving)
    {
        if (animator == null) return;

        animator.SetBool(IsMovingHash, moving);
    }
}