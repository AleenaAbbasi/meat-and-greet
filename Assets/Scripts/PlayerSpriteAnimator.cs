using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class PlayerSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private TopDownPlayerMovement movement;

   
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (movement == null)
            movement = GetComponentInParent<TopDownPlayerMovement>();
    }

    //the player now uses the animator tool assw wee removed the script use to it (it was easy this way)
    private void Update()
    {
        if (animator == null || movement == null)
            return;

        Vector2 input = movement.MoveInput;
        animator.SetFloat("Speed", input.magnitude);

        //only update direction while moving, so idle keeps the last facing.
        if (input.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("MoveX", input.x);
            animator.SetFloat("MoveY", input.y);
        }
    }
}