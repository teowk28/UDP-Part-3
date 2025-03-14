using UnityEngine;

// Interface for all interactable objects in the game
public interface IInteractable
{
    // Method called when player interacts with this object
    void Interact(GameObject player);

}