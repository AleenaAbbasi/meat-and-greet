using UnityEngine;

// Put this on the service counter. Press E near it to pick up a plate.
public class PlateCounter : MonoBehaviour, Interactable
{
    // Only works when the player's hands are empty (one plate at a time).
    public bool CanInteract(PlayerInteractor player)
    {
        return !player.holdingPlate;
    }

    public void Interact(PlayerInteractor player)
    {
        player.holdingPlate = true;
        Debug.Log("[Player] Picked up a " + OrderManager.Dish);
    }
}
