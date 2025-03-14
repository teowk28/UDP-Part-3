using UnityEngine;
using System.Collections;

public class ShopkeeperInteractable : MonoBehaviour, IInteractable
{
    // References 
    private EquipmentManager equipmentManager;

    private void Start()
    {
        // Find the managers we need
        equipmentManager = FindAnyObjectByType<EquipmentManager>();

        if (equipmentManager == null)
        {
            Debug.LogError("EquipmentManager not found in scene!");
        }
    }

    public void Interact(GameObject player)
    {
        Debug.Log("Interacting with shopkeeper!");

        // Get the player controller
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found on player object!");
            return;
        }

        // Set this shopkeeper as the last interacted with object
        playerController.SetLastInteractable(this);

        // The state machine will handle the transition to the BuySellMenuState
        // and from there to other states as needed
    }

    // Helper method to get the equipment manager
    // This is used by the state machine
    public EquipmentManager GetEquipmentManager()
    {
        return equipmentManager;
    }
}