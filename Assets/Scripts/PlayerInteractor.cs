using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float radius = 1.2f;

    // For delivery later: the one item the player is carrying.
    public string HeldItem { get; set; }

    private void Update()
    {
        if (!InteractPressed()) return;

        var target = FindNearest();
        if (target == null)
        {
            Debug.Log("[Player] Nothing to interact with");
            return;
        }
        target.Interact(this);
    }

    private Interactable FindNearest()
    {
        Interactable best = null;
        float bestDist = float.PositiveInfinity;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            var it = hit.GetComponentInParent<Interactable>();
            if (it == null || !it.CanInteract(this)) continue;

            float d = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = it; }
        }
        return best;
    }

    private static bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}