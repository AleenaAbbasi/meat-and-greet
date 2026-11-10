using UnityEngine;
using UnityEngine.InputSystem;

// On the Player.
// E = use the closest thing nearby (counter: pick up plate, customer: serve).
// Q = clean the closest dirty table.
public class PlayerInteractor : MonoBehaviour
{
    public float radius = 1.2f;

    // Is the player carrying a plate? (Only one at a time.)
    public bool holdingPlate;

    CafeCustomerSpawner spawner;

    void Start()
    {
        spawner = FindFirstObjectByType<CafeCustomerSpawner>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            PressE();

        if (Keyboard.current.qKey.wasPressedThisFrame)
            PressQ();
    }

    void PressE()
    {
        Interactable target = FindClosest();
        if (target != null)
            target.Interact(this);
        else
            Debug.Log("[Player] Nothing to interact with");
    }

    void PressQ()
    {
        bool cleaned = spawner != null && spawner.CleanTableNear(transform.position, radius);
        if (!cleaned)
            Debug.Log("[Table] No dirty table nearby");
    }

    // Find the closest object in range that can be used right now.
    Interactable FindClosest()
    {
        Interactable closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Interactable thing = hit.GetComponentInParent<Interactable>();
            if (thing == null || !thing.CanInteract(this)) continue;

            float distance = Vector2.Distance(hit.transform.position, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = thing;
            }
        }
        return closest;
    }

    // Shows the reach circle in the Scene view when the Player is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
