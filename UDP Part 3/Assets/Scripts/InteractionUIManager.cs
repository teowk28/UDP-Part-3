using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class InteractionUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // UI element references
    private VisualElement root;
    private VisualElement interactionPanel;  
    private VisualElement buySellMenu;       
    private VisualElement buySellBox;        
    private VisualElement confirmCancelPanel; 
    private VisualElement buyMenu;

    // Buy/Sell labels
    private Label buyLabel;
    private Label sellLabel;

    // Currently selected option (0 = buy, 1 = sell)
    private int selectedOption = 0;

    // Callbacks
    private System.Action onBuyConfirmed;
    private System.Action onSellConfirmed;
    private System.Action onMenuCancelled;

    private void Awake()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument reference is missing!");
            return;
        }

        // Get the root element
        root = uiDocument.rootVisualElement;

        // Find UI elements 
        interactionPanel = root.Q("InteractionButtonPanel"); 
        buySellMenu = root.Q("BuySellMenu");       
        buyMenu = root.Q("BuyMenu");
        buySellBox = buySellMenu?.Q("BuySellBox");          
        confirmCancelPanel = buyMenu.Q("ConfirmCancelButtonPanel"); 

        if (buySellBox != null)
        {
            buyLabel = buySellBox.Q<Label>("Buy");
            sellLabel = buySellBox.Q<Label>("Sell");
        }

        // Hide all UI elements initially
        HideAllMenus();
    }

    private void HideAllMenus()
    {
        ForceElementVisibility(interactionPanel, false);
        ForceElementVisibility(buySellMenu, false);
        ForceElementVisibility(confirmCancelPanel, false);
    }

    // Show the interaction button panel when near an interactable
    public void ShowInteractionUI(Vector3 worldPosition)
    {
        if (interactionPanel == null)
            return;

        ForceElementVisibility(interactionPanel, true);

        VisualElement parent = interactionPanel.parent;
        while (parent != null)
        {
            ForceElementVisibility(parent, true);
            parent = parent.parent;
        }
    }

    public void HideInteractionUI()
    {
        if (interactionPanel == null) return;
        ForceElementVisibility(interactionPanel, false);
    }

    // Show the buy/sell menu
    public void ShowBuySellMenu(System.Action buyCallback, System.Action sellCallback, System.Action cancelCallback)
    {
        onBuyConfirmed = buyCallback;
        onSellConfirmed = sellCallback;
        onMenuCancelled = cancelCallback;

        HideInteractionUI();

        if (buySellMenu == null)
            buySellMenu = root.Q("BuySellMenu");

        if (buySellMenu != null)
        {
            ForceElementVisibility(buySellMenu, true);

            foreach (var child in buySellMenu.Children())
            {
                ForceElementVisibility(child, true);
            }

            if (buySellBox != null)
            {
                buyLabel = buySellBox.Q<Label>("Buy");
                sellLabel = buySellBox.Q<Label>("Sell");

                if (buyLabel != null && sellLabel != null)
                    SelectOption(0);
            }

        }
        else
        {
            Debug.LogError("Still couldn't find BuySellMenu!");
        }
    }

    // Hide the buy/sell menu
    public void HideBuySellMenu()
    {
        if (buySellMenu == null) return;
        ForceElementVisibility(buySellMenu, false);
    }

    public void ShowConfirmCancelPanel(string confirmText = "Confirm", string cancelText = "Cancel", bool showCancelButton = true)
    {
        if (confirmCancelPanel == null) return;
        ForceElementVisibility(confirmCancelPanel, true);
        // SetButtonText(confirmText, cancelText, showCancelButton);
    }

    public void HideConfirmCancelPanel()
    {
        if (confirmCancelPanel == null) return;
        ForceElementVisibility(confirmCancelPanel, false);
    }

    // Select a menu option (0 = buy, 1 = sell)
    private void SelectOption(int option)
    {
        if (buyLabel == null || sellLabel == null)
            return;

        selectedOption = option;
        VisualElement pointer = buySellBox.Q<VisualElement>("Pointer");

        if (option == 0)
        {
            // Buy is selected
            buyLabel.style.color = new Color(1, 1, 0); 
            sellLabel.style.color = new Color(1, 1, 1);
            pointer.style.left = 96;

        }
        else
        {
            // Sell is selected
            buyLabel.style.color = new Color(1, 1, 1); 
            sellLabel.style.color = new Color(1, 1, 0);
            pointer.style.left = 288;
        }
    }

    // Handle confirm button press
    public void OnConfirmKeyPressed()
    {
        if (selectedOption == 0)
            onBuyConfirmed?.Invoke();
        else
            onSellConfirmed?.Invoke();
    }

    // Handle cancel button press
    public void OnCancelKeyPressed()
    {
        onMenuCancelled?.Invoke();
        HideBuySellMenu();
    }

    public void SetCancelButtonEnabled(bool enabled)
    {
        if (confirmCancelPanel == null) return;

        VisualElement cancelButton = confirmCancelPanel.Q("CancelButton");
        if (cancelButton == null) return;

        VisualElement cancelLeft = cancelButton.Q("CancelLeft");
        VisualElement cancelMid = cancelButton.Q("CancelMid");
        VisualElement cancelRight = cancelButton.Q("CancelRight");
        Label cancelText = cancelButton.Q<Label>("CancelText");
        Label lText = cancelButton.Q<Label>("LText");

        if (enabled)
        {
            if (cancelLeft != null) cancelLeft.style.unityBackgroundImageTintColor = Color.white;
            if (cancelMid != null) cancelMid.style.unityBackgroundImageTintColor = Color.white;
            if (cancelRight != null) cancelRight.style.unityBackgroundImageTintColor = Color.white;
            if (cancelText != null) cancelText.style.color = Color.white;
            if (lText != null) lText.style.color = Color.white;
        }
        else
        {
            Color disabledTint = new Color(0.6f, 0.6f, 0.6f, 1f);
            if (cancelLeft != null) cancelLeft.style.unityBackgroundImageTintColor = disabledTint;
            if (cancelMid != null) cancelMid.style.unityBackgroundImageTintColor = disabledTint;
            if (cancelRight != null) cancelRight.style.unityBackgroundImageTintColor = disabledTint;
            if (cancelText != null) cancelText.style.color = disabledTint;
            if (lText != null) lText.style.color = disabledTint;
        }
        cancelButton.MarkDirtyRepaint();
    }

    // Handle left/right navigation in the buy/sell menu
    public void OnLeftRightKeyPressed(bool right)
    {
        if (right && selectedOption == 0)
            SelectOption(1); 
        else if (!right && selectedOption == 1)
            SelectOption(0); 
    }

    public void SetButtonText(string confirmText, string cancelText, bool showCancelButton = true)
    {
        if (confirmCancelPanel == null) return;

        VisualElement confirmButton = confirmCancelPanel.Q("ConfirmButton");
        VisualElement cancelButton = confirmCancelPanel.Q("CancelButton");
        //VisualElement qButtonUI = confirmCancelPanel.Q("QButtonUI");
        Label confirmTextLabel = confirmButton?.Q<Label>("ConfirmText");
        Label cancelTextLabel = cancelButton?.Q<Label>("CancelText");

        if (confirmTextLabel != null)
            confirmTextLabel.text = confirmText;

        if (cancelTextLabel != null)
            cancelTextLabel.text = cancelText;

        if (cancelButton != null)
        {
            ForceElementVisibility(cancelButton, showCancelButton);
            //ForceElementVisibility(qButtonUI, showCancelButton);
            cancelButton.SetEnabled(showCancelButton);
            //qButtonUI.SetEnabled(showCancelButton);
        }
    }

    private void ForceElementVisibility(VisualElement element, bool visible)
    {
        if (element == null) return;

        element.style.display = StyleKeyword.Null;
        element.style.visibility = StyleKeyword.Null;
        element.style.opacity = StyleKeyword.Null;

        element.visible = visible;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        element.style.opacity = visible ? 1 : 0;

        element.MarkDirtyRepaint();
    }

}