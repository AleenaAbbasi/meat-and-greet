public interface Interactable
{
    bool CanInteract(PlayerInteractor player);
    void Interact(PlayerInteractor player);
}