using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Base class for all interaction states
public abstract class InteractionState
{
    protected PlayerController player;
    protected InteractionStateMachine stateMachine;

    public InteractionState(PlayerController player, InteractionStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void HandleLKeyPressed() { }
    public virtual void HandleQKeyPressed() { }
    public virtual void HandleKKeyPressed() { }
    public virtual void HandleAKeyPressed() { }
    public virtual void HandleDKeyPressed() { }
    public virtual void HandlePKeyPressed() { }
}

// The main state machine that will manage state transitions
public class InteractionStateMachine : MonoBehaviour
{
    private PlayerController playerController;
    private InteractionState currentState;
    private InputHandler inputHandler;

    // References to UI managers
    private InteractionUIManager uiManager;
    private ShopUIManager shopUIManager;

    // Dictionary to store all possible states
    private Dictionary<System.Type, InteractionState> states = new Dictionary<System.Type, InteractionState>();

    private int lastBuySellOption = 0;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // Find UI managers
        uiManager = FindAnyObjectByType<InteractionUIManager>();
        shopUIManager = FindAnyObjectByType<ShopUIManager>();

        if (uiManager == null)
            Debug.LogError("InteractionUIManager not found in scene!");

        inputHandler = InputHandler.Instance;

        if (shopUIManager == null)
            Debug.LogError("ShopUIManager not found in scene!");

        // Initialize all possible states
        states[typeof(ExplorationState)] = new ExplorationState(playerController, this);
        states[typeof(NearInteractableState)] = new NearInteractableState(playerController, this);
        states[typeof(InitialInteractionState)] = new InitialInteractionState(playerController, this);
        states[typeof(BuySellMenuState)] = new BuySellMenuState(playerController, this);
        states[typeof(BuyMenuState)] = new BuyMenuState(playerController, this);
        states[typeof(SellMenuState)] = new SellMenuState(playerController, this);

        // Set initial state
        ChangeState<ExplorationState>();
    }

    public void Update()
    {
        if (currentState != null)
            currentState.Update();

        if (inputHandler.InteractPressed())
            currentState.HandleLKeyPressed();

        if (inputHandler.TabLeftPressed())
            currentState.HandleQKeyPressed();

        if (inputHandler.CancelPressed())
            currentState.HandleKKeyPressed();

        if (inputHandler.LeftPressed())
            currentState.HandleAKeyPressed();

        if (inputHandler.RightPressed())
            currentState.HandleDKeyPressed();

        if (inputHandler.TabRightPressed())
            currentState.HandlePKeyPressed();
    }

    public void FixedUpdate()
    {
        if (currentState != null)
            currentState.FixedUpdate();
    }

    public void ChangeState<T>() where T : InteractionState
    {
        if (currentState != null)
        {
            Debug.Log($"Exiting state: {currentState.GetType().Name}");
            currentState.ExitState();
        }

        currentState = states[typeof(T)];
        Debug.Log($"Entering state: {currentState.GetType().Name}");
        currentState.EnterState();
    }

    public int GetLastBuySellOption()
    {
        return lastBuySellOption;
    }

    public void SetLastBuySellOption(int option)
    {
        lastBuySellOption = option;
    }

    public InteractionUIManager GetUIManager() => uiManager;
    public ShopUIManager GetShopUIManager() => shopUIManager;
}

// Exploration state - player is free to move around
public class ExplorationState : InteractionState
{
    public ExplorationState(PlayerController player, InteractionStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void EnterState()
    {
        player.SetMovementEnabled(true);
    }

    public override void Update()
    {
        // Check if player is near an interactable
        if (player.HasNearbyInteractable())
        {
            stateMachine.ChangeState<NearInteractableState>();
        }
    }
}

// Near Interactable state - player is close to something they can interact with
public class NearInteractableState : InteractionState
{
    private GameObject nearestInteractable;

    public NearInteractableState(PlayerController player, InteractionStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void EnterState()
    {
        nearestInteractable = player.GetNearestInteractable();

        // Show the interaction UI
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.ShowInteractionUI(nearestInteractable.transform.position);
        }
    }

    public override void ExitState()
    {
        // Hide the interaction UI
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.HideInteractionUI();
        }
    }

    public override void Update()
    {
        // Check if player has moved away from the interactable
        if (!player.HasNearbyInteractable())
        {
            stateMachine.ChangeState<ExplorationState>();
            return;
        }

        // Check if the closest interactable has changed
        GameObject current = player.GetNearestInteractable();
        if (current != nearestInteractable)
        {
            nearestInteractable = current;

            // Update UI position
            var uiManager = stateMachine.GetUIManager();
            if (uiManager != null)
            {
                uiManager.ShowInteractionUI(nearestInteractable.transform.position);
            }
        }
    }

    public override void HandleLKeyPressed()
    {
        if (player.IsFacingInteractable(nearestInteractable))
        {
            player.FaceInteractable(nearestInteractable.transform.position);

            IInteractable interactable = nearestInteractable.GetComponent<IInteractable>();
            if (interactable != null)
            {

                player.SetLastInteractable(interactable);

                stateMachine.ChangeState<InitialInteractionState>();

                interactable.Interact(player.gameObject);
            }
            else
            {
                Debug.LogError("No IInteractable component found on object");
            }
        }
        else
        {
            Debug.Log("Player is not facing the interactable");
        }
    }
}

// Initial Interaction state - the starting point of any interaction
public class InitialInteractionState : InteractionState
{
    public InitialInteractionState(PlayerController player, InteractionStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void EnterState()
    {
        player.SetMovementEnabled(false);

        // Check what type of interactable we're dealing with
        IInteractable lastInteractable = player.GetLastInteractable();

        if (lastInteractable is ShopkeeperInteractable)
        {
            // If it's a shopkeeper, immediately transition to BuySellMenuState
            stateMachine.ChangeState<BuySellMenuState>();
        }
        else
        {
            Debug.Log($"Last interactable is not a shopkeeper: {lastInteractable?.GetType().Name}");
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exiting InitialInteractionState");
    }
}

// BuySell Menu state - player is choosing between Buy and Sell options
public class BuySellMenuState : InteractionState
{
    private ShopkeeperInteractable shopkeeper;

    public BuySellMenuState(PlayerController player, InteractionStateMachine stateMachine) : base(player, stateMachine) { }

    public override void EnterState()
    {
        shopkeeper = player.GetLastInteractable() as ShopkeeperInteractable;
        var uiManager = stateMachine.GetUIManager();

        if (uiManager != null)
        {
            uiManager.ShowConfirmCancelPanel();

            uiManager.ShowConfirmCancelPanel("Confirm", "Cancel");

            int currentOption = ((InteractionStateMachine)stateMachine).GetLastBuySellOption();

            player.StartCoroutine(DelayedShowBuySellMenu(uiManager, currentOption));
        }
        else
        {
            Debug.LogError("UIManager is null in BuySellMenuState!");
        }
    }

    private IEnumerator DelayedShowBuySellMenu(InteractionUIManager uiManager, int initialOption)
    {
        yield return new WaitForSeconds(0);

        Debug.Log("Showing BuySellMenu after delay");
        uiManager.ShowBuySellMenu(
            OnBuySelected,
            OnSellSelected,
            OnCancelled,
            initialOption
        );
    }

    public override void ExitState()
    {
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.HideBuySellMenu();
            uiManager.HideConfirmCancelPanel();
        }
    }

    private void OnBuySelected()
    {
        ((InteractionStateMachine)stateMachine).SetLastBuySellOption(0);
        stateMachine.ChangeState<BuyMenuState>();
    }

    private void OnSellSelected()
    {
        ((InteractionStateMachine)stateMachine).SetLastBuySellOption(1);
        stateMachine.ChangeState<SellMenuState>();
    }

    private void OnCancelled()
    {
        // Return to exploration state
        stateMachine.ChangeState<ExplorationState>();
    }

    public override void HandleLKeyPressed()
    {
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.OnConfirmKeyPressed();
        }
    }

    public override void HandleKKeyPressed()
    {
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.OnCancelKeyPressed();
        }
    }

    public override void HandleAKeyPressed()
    {
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.OnLeftRightKeyPressed(false);
        }
    }

    public override void HandleDKeyPressed()
    {
        var uiManager = stateMachine.GetUIManager();
        if (uiManager != null)
        {
            uiManager.OnLeftRightKeyPressed(true);
        }
    }
}

// Buy Menu state - player is browsing items to purchase
public class BuyMenuState : InteractionState
{
    private EquipmentManager equipmentManager;

    public BuyMenuState(PlayerController player, InteractionStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void EnterState()
    {
        equipmentManager = Object.FindAnyObjectByType<EquipmentManager>();

        var uiManager = stateMachine.GetUIManager();
        var shopUIManager = stateMachine.GetShopUIManager();

        if (uiManager != null)
        {
            uiManager.HideBuySellMenu();
            uiManager.ShowConfirmCancelPanel("Buy", "Back");
        }

        if (shopUIManager != null && equipmentManager != null)
        {
            shopUIManager.ShowEquipmentBuyMenu(
                equipmentManager.GetBuyableEquipmentItems(),  
                equipmentManager.GetPlayerGold(),
                OnItemPurchased,
                OnBuyMenuCancelled,
                OnTabChanged
            );
        }
    }

    public override void ExitState()
    {
        var uiManager = stateMachine.GetUIManager();
        var shopUIManager = stateMachine.GetShopUIManager();

        if (shopUIManager != null)
        {
            shopUIManager.HideBuyMenu();
        }

        if (uiManager != null)
        {
            uiManager.HideConfirmCancelPanel();
        }
    }

    private bool OnItemPurchased(EquipmentItem item)
    {
        if (item.ownedQuantity >= 999)
        {
            var shopUIManager = stateMachine.GetShopUIManager();
            if (shopUIManager != null)
                shopUIManager.ShowInventoryLimitMessage();
            return false;
        }
        if (equipmentManager.PurchaseItem(item))
        {
            var shopUIManager = stateMachine.GetShopUIManager();
            if (shopUIManager != null)
                shopUIManager.UpdateItemDetails(item, equipmentManager.GetPlayerGold());
            return true;
        }
        else
        {
            var shopUIManager = stateMachine.GetShopUIManager();
            if (shopUIManager != null)
                shopUIManager.ShowNotEnoughGoldMessage();
            return false;
        }
    }

    private void OnTabChanged(bool isEquipmentTab)
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (isEquipmentTab)
                shopUIManager.ShowEquipmentItems(equipmentManager.GetBuyableEquipmentItems());
            else
                shopUIManager.ShowRegularItems(equipmentManager.GetBuyableRegularItems());
        }
    }

    private void OnBuyMenuCancelled()
    {
        stateMachine.ChangeState<BuySellMenuState>();
    }

    public override void HandleLKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        var uiManager = stateMachine.GetUIManager();

        if (shopUIManager != null)
        {
            if (shopUIManager.IsInInsufficientFundsState() ||
                shopUIManager.IsInRestrictedItemState() ||
                shopUIManager.IsInInventoryLimitState())
            {
                if (uiManager != null)
                {
                    uiManager.SetCancelButtonEnabled(false);
                }
            }

            // Always call OnConfirmClicked to handle the confirmation
            shopUIManager.OnConfirmClicked();
        }
    }

    public override void HandleQKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (!shopUIManager.IsInPurchaseState())
            {
                if (!shopUIManager.IsEquipmentTabSelected())
                    shopUIManager.OnTabKeyPressed(false);
            }    
        }
    }

    public override void HandleKKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (shopUIManager.IsInPurchaseState())
            {
                if (!shopUIManager.IsInInsufficientFundsState() && !shopUIManager.IsInRestrictedItemState() && !shopUIManager.IsInInventoryLimitState())
                    shopUIManager.OnCancelClicked();
            }
            else
            {
                shopUIManager.OnCancelClicked();
            }
        }
    }

    public override void HandleAKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
            shopUIManager.OnLeftRightKeyPressed(false);
    }

    public override void HandleDKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
            shopUIManager.OnLeftRightKeyPressed(true);
    }

    public override void HandlePKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (!shopUIManager.IsInPurchaseState())
            {
                if (shopUIManager.IsEquipmentTabSelected())
                    shopUIManager.OnTabKeyPressed(true);
            }    
        }
    }
}

// Sell Menu state - player is selecting items to sell
public class SellMenuState : InteractionState
{
    private EquipmentManager equipmentManager;

    public SellMenuState(PlayerController player, InteractionStateMachine stateMachine)
        : base(player, stateMachine) { }

    public override void EnterState()
    {
        equipmentManager = Object.FindAnyObjectByType<EquipmentManager>();

        var uiManager = stateMachine.GetUIManager();
        var shopUIManager = stateMachine.GetShopUIManager();

        if (uiManager != null)
        {
            uiManager.HideBuySellMenu();
            uiManager.ShowConfirmCancelPanel("Sell", "Back");
        }

        if (shopUIManager != null && equipmentManager != null)
        {
            // Create filtered lists of only owned items
            List<EquipmentItem> ownedEquipment = equipmentManager.GetEquipmentItems()
            .Where(item => item.ownedQuantity > 0 || item.cost == 0)
            .ToList();

            List<EquipmentItem> ownedRegularItems = equipmentManager.GetRegularItems()
                .Where(item => item.ownedQuantity > 0 || item.cost == 0)
                .ToList();

            // Show sell menu with owned items
            shopUIManager.ShowEquipmentSellMenu(
                ownedEquipment,
                ownedRegularItems,
                equipmentManager.GetPlayerGold(),
                OnItemSold,
                OnSellMenuCancelled,
                OnTabChanged
            );
        }
    }

    public override void ExitState()
    {
        var uiManager = stateMachine.GetUIManager();
        var shopUIManager = stateMachine.GetShopUIManager();

        if (shopUIManager != null)
        {
            shopUIManager.HideSellMenu();

            shopUIManager.ShowItemDetailsUI();
        }

        if (uiManager != null)
        {
            uiManager.HideConfirmCancelPanel();
        }
    }

    private bool OnItemSold(EquipmentItem item)
    {
        if (item.cost == 0)       
            return false;

        if (equipmentManager.SellItem(item))
        {
            var shopUIManager = stateMachine.GetShopUIManager();
            if (shopUIManager != null)
                shopUIManager.UpdateItemDetails(item, equipmentManager.GetPlayerGold());
            return true;
        }
        return false;
    }

    private void OnTabChanged(bool isEquipmentTab)
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            // Create filtered lists of only owned items
            List<EquipmentItem> ownedEquipment = equipmentManager.GetEquipmentItems()
            .Where(item => item.ownedQuantity > 0 || item.cost == 0)
            .ToList();

            List<EquipmentItem> ownedRegularItems = equipmentManager.GetRegularItems()
                .Where(item => item.ownedQuantity > 0 || item.cost == 0)
                .ToList();

            if (isEquipmentTab)
                shopUIManager.ShowSellEquipmentItems(ownedEquipment);
            else
                shopUIManager.ShowSellRegularItems(ownedRegularItems);
        }
    }

    private void OnSellMenuCancelled()
    {
        stateMachine.ChangeState<BuySellMenuState>();
    }

    public override void HandleLKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        var uiManager = stateMachine.GetUIManager();

        if (shopUIManager != null)
        {
            // Check if we need to disable cancel button
            if (shopUIManager.IsInInsufficientFundsState() ||
                shopUIManager.IsInRestrictedItemState() ||
                shopUIManager.IsInInventoryLimitState())
            {
                if (uiManager != null)
                {
                    uiManager.SetCancelButtonEnabled(false);
                }
            }

            shopUIManager.OnConfirmClicked();
        }
    }

    public override void HandleQKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (!shopUIManager.IsInSellState())
            {
                if (!shopUIManager.IsEquipmentTabSelected())
                    shopUIManager.OnTabKeyPressed(false);
            }
        }
    }

    public override void HandleKKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (shopUIManager.IsInRestrictedItemState())
                return;

            shopUIManager.OnCancelClicked();
        }
    }

    public override void HandleAKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
            shopUIManager.OnLeftRightKeyPressed(false);
    }

    public override void HandleDKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
            shopUIManager.OnLeftRightKeyPressed(true);
    }

    public override void HandlePKeyPressed()
    {
        var shopUIManager = stateMachine.GetShopUIManager();
        if (shopUIManager != null)
        {
            if (!shopUIManager.IsInSellState())
            {
                if (shopUIManager.IsEquipmentTabSelected())
                    shopUIManager.OnTabKeyPressed(true);
            }
        }
    }
}