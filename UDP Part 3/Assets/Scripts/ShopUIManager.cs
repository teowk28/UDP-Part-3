﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using static UnityEditor.Timeline.TimelinePlaybackControls;
using System.Linq;

public class ShopUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // Circle display settings
    [SerializeField] private Vector2 circleCenter = new Vector2(400, 400);
    [SerializeField] private float horizontalRadius = 220f; // Width of the oval
    [SerializeField] private float verticalRadius = 120f;   // Height of the oval

    // UI elements
    private VisualElement root;
    private VisualElement buyMenu;
    private VisualElement equipmentTab;
    private VisualElement itemTab;
    private VisualElement selectionBox;
    private VisualElement whatWillYouBuyBox;
    private VisualElement inventory;

    // Character Usability Icons Container
    private VisualElement applicableContainer;
    private VisualElement randiIcon;
    private VisualElement popoiIcon;
    private VisualElement purimIcon;

    // Detail elements
    private Label itemNameText;
    private Label itemCostText;
    private Label usableByText;
    private Label itemDescText;
    private Label ownededText;
    private Label ownedQuantityText;
    private Label goldText;
    private Label whatWillYouBuyText;

    // Buttons
    private Button confirmButton;
    private Button cancelButton;
    private Button equipmentTabButton;
    private Button itemTabButton;

    // Item data
    private List<EquipmentItem> currentItems;
    private int selectedItemIndex = 0;
    private bool isEquipmentTabSelected = true;
    private EquipmentItem currentSelectedItem;

    // Item positions in the circle
    private List<Vector2> circularPositions = new List<Vector2>();

    // Callbacks
    private Func<EquipmentItem, bool> onItemPurchased;
    private Func<EquipmentItem, bool> onItemSold;
    private Action onCancelled;
    private Action<bool> onTabChanged;

    // Reference to pre-built equipment and items elements in UI Builder
    private List<VisualElement> equipmentUIElements = new List<VisualElement>();
    private List<VisualElement> regularItemUIElements = new List<VisualElement>();

    // Current rotation offset index (which position is at the "front")
    private int rotationOffset = 0;
    private float animationTime = 0f;
    private Color nonSelectedTint = new Color(0.6f, 0.6f, 0.6f, 1f); // Darkened color for non-selected items
    private Color normalTint = Color.white; // Normal color for selected items

    // Dialog state
    private bool isConfirmingPurchase = false;
    private bool insufficientFunds = false;
    private bool showingPurchaseSuccess = false;
    private bool showingInsufficientFunds = false;
    private bool showingRestrictedItem = false;
    private bool isConfirmingSell = false;
    private bool showingSellSuccess = false;
    private bool isInSellMode = false;

    private List<VisualElement> activeItemUIElements = new List<VisualElement>();

    public bool IsInInsufficientFundsState() => showingInsufficientFunds;
    public bool IsInRestrictedItemState() => showingRestrictedItem;

    private void Awake()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument reference is missing!");
            return;
        }

        root = uiDocument.rootVisualElement;

        // Find UI elements
        buyMenu = root.Q("BuyMenu");
        equipmentTab = buyMenu?.Q("EquipmentTab");
        itemTab = buyMenu?.Q("ItemTab");
        inventory = buyMenu?.Q("Inventory");
        selectionBox = root.Q("SelectionBox");
        whatWillYouBuyBox = buyMenu?.Q("WhatWillYouBuyBox");

        applicableContainer = buyMenu?.Q("Applicable");
        randiIcon = applicableContainer.Q("RandiIcon");
        popoiIcon = applicableContainer.Q("PopoiIcon");
        purimIcon = applicableContainer.Q("PurimIcon");

        // Get text elements
        itemNameText = buyMenu?.Q<Label>("EquipmentNameText");
        itemCostText = buyMenu?.Q<Label>("EquipmentCost");
        usableByText = buyMenu?.Q<Label>("UableByText");
        itemDescText = buyMenu?.Q<Label>("DescText");
        ownededText = buyMenu?.Q<Label>("OwnedText");
        ownedQuantityText = buyMenu?.Q<Label>("OwnedQuantity");
        goldText = buyMenu?.Q<Label>("GoldText"); 
        whatWillYouBuyText = whatWillYouBuyBox?.Q<Label>("Question");

        // Get buttons
        confirmButton = buyMenu?.Q<Button>("ConfirmButton");
        cancelButton = buyMenu?.Q<Button>("CancelButton");
        equipmentTabButton = buyMenu?.Q<Button>("LButtonTab");
        itemTabButton = buyMenu?.Q<Button>("RButtonTab");

        // Register button events
        if (confirmButton != null)
            confirmButton.clicked += OnConfirmClicked;

        if (cancelButton != null)
            cancelButton.clicked += OnCancelClicked;

        if (equipmentTabButton != null)
            equipmentTabButton.clicked += () => SwitchTab(true);

        if (itemTabButton != null)
            itemTabButton.clicked += () => SwitchTab(false);

        if (whatWillYouBuyText != null)
            whatWillYouBuyText.text = "What'll you be\nbuying?";

        // Hide the menu initially
        HideAllMenus();

        // Start the animation coroutine
        StartCoroutine(AnimateSelectionBox());

        FindAllItemUIElements();

        // Set equipment elements as the initial active collection
        activeItemUIElements = equipmentUIElements;

        CalculateCircularPositions(activeItemUIElements.Count);
    }

    public bool IsEquipmentTabSelected()
    {
        return isEquipmentTabSelected;
    }

    public bool IsInPurchaseState()
    {
        // Return true if we're in any state where navigation should be locked
        return isConfirmingPurchase || isConfirmingSell || insufficientFunds ||
           showingInsufficientFunds || showingPurchaseSuccess ||
           showingRestrictedItem || showingSellSuccess;
    }

    public bool IsInSellState()
    {
        return isConfirmingSell || showingSellSuccess;
    }

    private void UpdateCharacterUsability(EquipmentItem item)
    {
        if (item == null)
            return;

        if (applicableContainer == null || randiIcon == null || popoiIcon == null || purimIcon == null)
        {
            Debug.LogError($"Character icons not found: Container={applicableContainer != null}, " +
                          $"Randi={randiIcon != null}, Popoi={popoiIcon != null}, Purim={purimIcon != null}");
            return;
        }

        ForceElementVisibility(applicableContainer, true);

        // Set opacity based on usability - 1.0 for usable, 0.5 for not usable
        float unusableOpacity = 0.5f;

        randiIcon.style.opacity = item.usableByRandi ? 1.0f : unusableOpacity;
        popoiIcon.style.opacity = item.usableByPopoi ? 1.0f : unusableOpacity;
        purimIcon.style.opacity = item.usableByPurim ? 1.0f : unusableOpacity;

        applicableContainer.MarkDirtyRepaint();
    }

    private IEnumerator AnimateSelectionBox()
    {
        List<VisualElement> cornerPieces = new List<VisualElement>();
        if (selectionBox != null)
        {
            foreach (var child in selectionBox.Children())
            {
                cornerPieces.Add(child);
                ForceElementVisibility(child, true);
            }
        }

        Vector2 fixedPosition = new Vector2(circleCenter.x, circleCenter.y + verticalRadius);

        while (true)
        {
            if (selectionBox != null && selectionBox.visible && selectedItemIndex >= 0 &&
                activeItemUIElements != null && selectedItemIndex < activeItemUIElements.Count)
            {
                VisualElement selectedElement = activeItemUIElements[selectedItemIndex];

                float selectedItemWidth = selectedElement.resolvedStyle.width;
                float selectedItemHeight = selectedElement.resolvedStyle.height;

                float margin = 14f;

                float extraSpace = 25f;
                float selectionBoxWidth = selectedItemWidth + (margin * 2) + extraSpace;
                float selectionBoxHeight = selectedItemHeight + (margin * 2) + extraSpace;

                selectionBox.style.position = Position.Absolute;
                selectionBox.style.width = selectionBoxWidth;
                selectionBox.style.height = selectionBoxHeight;
                selectionBox.style.left = fixedPosition.x - (selectionBoxWidth / 2);
                selectionBox.style.top = fixedPosition.y - (selectionBoxHeight / 2);

                selectionBox.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0);

                selectionBox.style.backgroundColor = new Color(0, 0, 0, 0);
                selectionBox.style.borderLeftWidth = 0;
                selectionBox.style.borderRightWidth = 0;
                selectionBox.style.borderTopWidth = 0;
                selectionBox.style.borderBottomWidth = 0;

                animationTime += Time.deltaTime * 2f;

                float pulse = 1.0f + 0.05f * Mathf.Sin(animationTime * 3f);
                selectionBox.style.scale = new Scale(new Vector3(pulse, pulse, 1));

                if (cornerPieces.Count >= 4)
                {
                    float cornerWidth = cornerPieces[0].resolvedStyle.width;
                    float cornerHeight = cornerPieces[0].resolvedStyle.height;

                    for (int i = 0; i < cornerPieces.Count; i++)
                    {
                        cornerPieces[i].style.position = Position.Absolute;
                        cornerPieces[i].style.backgroundColor = StyleKeyword.Null;
                    }

                    cornerPieces[0].style.left = 0;
                    cornerPieces[0].style.top = 0;

                    cornerPieces[1].style.left = selectionBoxWidth - cornerWidth;
                    cornerPieces[1].style.top = 0;

                    cornerPieces[2].style.left = 0;
                    cornerPieces[2].style.top = selectionBoxHeight - cornerHeight;

                    cornerPieces[3].style.left = selectionBoxWidth - cornerWidth;
                    cornerPieces[3].style.top = selectionBoxHeight - cornerHeight;
                }

                selectionBox.MarkDirtyRepaint();
            }

            yield return null;
        }
    }

    private void FindAllItemUIElements()
    {
        equipmentUIElements.Clear();
        regularItemUIElements.Clear();

        VisualElement itemContainer = buyMenu;

        if (itemContainer != null)
        {
            FindItemElementsRecursively(itemContainer);
        }
    }

    // 3. Modify your recursive search method to categorize elements into equipment or regular items
    private void FindItemElementsRecursively(VisualElement container)
    {
        foreach (var child in container.Children())
        {
            // Equipment items (keep your existing checks)
            if (child.name.Contains("TigerSuit") || child.name.Contains("FancyOveralls") ||
                child.name.Contains("KungFuSuit") || child.name.Contains("ElbowPad") ||
                child.name.Contains("LazuriRing") || child.name.Contains("MidgeRobe") ||
                child.name.Contains("DragonHelm") || child.name.Contains("RabiteCap") ||
                child.name.Contains("QuillCap") || child.name.Contains("RaccoonCap") ||
                child.name.Contains("Gauntlet") || child.name.Contains("Wristband"))
            {
                equipmentUIElements.Add(child);
            }
            // Regular items (new checks based on your item names)
            else if (child.name.Contains("Barrel") || child.name.Contains("Candy") ||
                     child.name.Contains("Chocolate") || child.name.Contains("Cup") ||
                     child.name.Contains("Faerie") || child.name.Contains("Flammie") ||
                     child.name.Contains("Magic") || child.name.Contains("Medical") ||
                     child.name.Contains("Midge") || child.name.Contains("Moogle") ||
                     child.name.Contains("Royal"))
            {
                regularItemUIElements.Add(child);
            }
            else if (child.childCount > 0)
            {
                FindItemElementsRecursively(child);
            }
        }
    }

    private void CalculateCircularPositions(int itemCount)
    {
        circularPositions.Clear();

        for (int i = 0; i < itemCount; i++)
        {
            float angle = ((float)i / itemCount * Mathf.PI * 2) + (Mathf.PI * 0.5f);

            float x = circleCenter.x + Mathf.Cos(angle) * horizontalRadius;
            float y = circleCenter.y + Mathf.Sin(angle) * verticalRadius;

            circularPositions.Add(new Vector2(x, y));
        }
    }

    public void ShowEquipmentBuyMenu(List<EquipmentItem> items, int goldAmount,
                           Func<EquipmentItem, bool> purchaseCallback,
                           Action cancelCallback,
                           Action<bool> tabChangeCallback)
    {
        isInSellMode = false;
        onItemPurchased = purchaseCallback;
        onCancelled = cancelCallback;
        onTabChanged = tabChangeCallback;

        currentItems = items;
        isEquipmentTabSelected = true;
        ForceElementVisibility(buyMenu, true);

        // Find all UI elements at the start if not done already
        if (equipmentUIElements.Count == 0 || regularItemUIElements.Count == 0)
            FindAllItemUIElements();

        HideAllItemElements();

        // Set equipment elements as active
        activeItemUIElements = equipmentUIElements;

        if (equipmentTab != null && itemTab != null)
        {
            ForceElementVisibility(equipmentTab, true);
            ForceElementVisibility(itemTab, true);
            UpdateTabVisuals();
        }

        if (activeItemUIElements.Count > 0)
        {
            CalculateCircularPositions(activeItemUIElements.Count);

            isConfirmingPurchase = false;
            insufficientFunds = false;
            showingPurchaseSuccess = false;
            showingInsufficientFunds = false;

            if (whatWillYouBuyText != null)
                whatWillYouBuyText.text = "What'll you be\nbuying?";

            UpdateGoldDisplay(goldAmount);

            selectedItemIndex = 0;
            rotationOffset = 0;

            UpdateCircularDisplay();

            if (currentItems.Count > 0)
                UpdateItemDetails(currentItems[0], goldAmount);
        }
        else
        {
            Debug.LogError("No item UI elements found! Cannot display circular menu.");
        }
    }


    private void UpdateTabVisuals()
    {
        if (equipmentTab == null || itemTab == null)
            return;

        // Define our colors
        Color selectedTabColor = Color.white; // Normal white color for selected tab
        Color unselectedTabColor = new Color(0.7f, 0.7f, 0.7f, 1f); // 30% darkened for unselected tab

        // Parse the hex color 424352 (dark slate blue/gray)
        Color textColor = new Color(
            (float)0x42 / 255f,
            (float)0x43 / 255f,
            (float)0x52 / 255f,
            1f
        );

        VisualElement[] equipmentTabParts = new[] {
        equipmentTab.Q("EquipmentTabLeft"),
        equipmentTab.Q("EquipmentTabMid"),
        equipmentTab.Q("EquipmentTabRight")
    };

        VisualElement[] itemTabParts = new[] {
        itemTab.Q("ItemsTabLeft"),
        itemTab.Q("ItemsTabMid"),
        itemTab.Q("ItemsTabRight")
    };

        // Apply colors to equipment tab parts
        Color equipmentColor = isEquipmentTabSelected ? selectedTabColor : unselectedTabColor;
        foreach (var part in equipmentTabParts)
        {
            if (part != null)
                part.style.unityBackgroundImageTintColor = equipmentColor;
        }

        // Apply colors to item tab parts
        Color itemColor = isEquipmentTabSelected ? unselectedTabColor : selectedTabColor;
        foreach (var part in itemTabParts)
        {
            if (part != null)
                part.style.unityBackgroundImageTintColor = itemColor;
        }

        // Apply scaling effect
        equipmentTab.style.scale = new Scale(
            new Vector3(isEquipmentTabSelected ? 1.1f : 1f,
                       isEquipmentTabSelected ? 1.1f : 1f, 1f));

        itemTab.style.scale = new Scale(
            new Vector3(!isEquipmentTabSelected ? 1.1f : 1f,
                       !isEquipmentTabSelected ? 1.1f : 1f, 1f));

        // Find all the text elements
        Label lEquipmentText = equipmentTab.Q<Label>("LEquipmentText");
        Label equipmentText = equipmentTab.Q<Label>("EquipmentText");
        Label rItemText = itemTab.Q<Label>("RItemText");
        Label itemText = itemTab.Q<Label>("ItemsText");

        // Set text colors based on which tab is selected
        if (isEquipmentTabSelected)
        {
            // Equipment tab is selected
            if (lEquipmentText != null) lEquipmentText.style.color = textColor; // Selected tab's text color
            if (equipmentText != null) equipmentText.style.color = Color.white; // Default color
            if (rItemText != null) rItemText.style.color = Color.white; // Default color
            if (itemText != null) itemText.style.color = textColor; // Unselected tab's text color
        }
        else
        {
            // Item tab is selected
            if (lEquipmentText != null) lEquipmentText.style.color = Color.white; // Default color
            if (equipmentText != null) equipmentText.style.color = textColor; // Unselected tab's text color
            if (rItemText != null) rItemText.style.color = textColor; // Selected tab's text color
            if (itemText != null) itemText.style.color = Color.white; // Default color
        }

        equipmentTab.MarkDirtyRepaint();
        itemTab.MarkDirtyRepaint();
    }
    public void ShowRegularItems(List<EquipmentItem> items)
    {
        currentItems = items;
        isEquipmentTabSelected = false;
        activeItemUIElements = regularItemUIElements;

        CalculateCircularPositions(activeItemUIElements.Count);
        UpdateTabVisuals();

        selectedItemIndex = 0;
        rotationOffset = 0;

        UpdateCircularDisplay();

        if (currentItems.Count > 0)
            UpdateItemDetails(currentItems[0], -1);
    }

    public void ShowEquipmentItems(List<EquipmentItem> items)
    {
        currentItems = items;
        isEquipmentTabSelected = true;
        activeItemUIElements = equipmentUIElements;

        CalculateCircularPositions(activeItemUIElements.Count);
        UpdateTabVisuals();

        selectedItemIndex = 0;
        rotationOffset = 0;

        UpdateCircularDisplay();

        if (currentItems.Count > 0)
            UpdateItemDetails(currentItems[0], -1);
    }

    private void UpdateCircularDisplay()
    {
        HideAllItemElements();

        if (activeItemUIElements.Count == 0 || currentItems == null || currentItems.Count == 0)
            return;

        if (circularPositions.Count != activeItemUIElements.Count)
            CalculateCircularPositions(activeItemUIElements.Count);

        for (int i = 0; i < activeItemUIElements.Count && i < currentItems.Count; i++)
        {
            int positionIndex = (i - selectedItemIndex + circularPositions.Count) % circularPositions.Count;
            Vector2 position = circularPositions[positionIndex];
            VisualElement itemElement = activeItemUIElements[i];

            bool isSelected = i == selectedItemIndex;

            ForceElementVisibility(itemElement, true);

            // Set size based on selection state
            float size = isSelected ? 96 : 64;
            itemElement.style.width = size;
            itemElement.style.height = size;

            // Apply tint based on selection state
            itemElement.style.unityBackgroundImageTintColor = isSelected ? normalTint : nonSelectedTint;

            // Position the element
            itemElement.style.position = Position.Absolute;
            itemElement.style.left = position.x - size / 2;
            itemElement.style.top = position.y - size / 2;
        }

        if (selectionBox != null)
        {
            if (selectedItemIndex >= 0 && selectedItemIndex < activeItemUIElements.Count)
                ForceElementVisibility(selectionBox, true);
            else
                ForceElementVisibility(selectionBox, false);
        }
    }

    private void HideAllItemElements()
    {
        foreach (var element in equipmentUIElements)
            ForceElementVisibility(element, false);

        foreach (var element in regularItemUIElements)
            ForceElementVisibility(element, false);
    }

    private void SelectItem(int index)
    {
        if (currentItems == null || currentItems.Count == 0 || activeItemUIElements == null || activeItemUIElements.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, currentItems.Count - 1);

        selectedItemIndex = index;

        currentSelectedItem = currentItems[index];

        rotationOffset = (activeItemUIElements.Count - index) % activeItemUIElements.Count;

        EquipmentItem selectedItem = currentItems[index];
        UpdateItemDetails(selectedItem, -1); // -1 means don't update gold

        UpdateCircularDisplay();

        if (!showingPurchaseSuccess && !showingInsufficientFunds && !showingSellSuccess)
        {
            isConfirmingPurchase = false;
            isConfirmingSell = false;

            if (whatWillYouBuyText != null)
            {
                whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
            }
        }
    }

    public void UpdateItemDetails(EquipmentItem item, int goldAmount)
    {
        if (item == null)
            return;

        currentSelectedItem = item;

        if (itemNameText != null)
            itemNameText.text = item.name;

        if (itemCostText != null)
        {
            if (isInSellMode)
            {
                if (item.cost == 0)
                {
                    itemCostText.text = "- GP";
                    itemCostText.style.color = new Color(1, 1, 1, 1);
                }
                else
                {
                    int sellPrice = Mathf.FloorToInt(item.cost / 2f);
                    itemCostText.text = $"{sellPrice} GP";
                    itemCostText.style.color = new Color(1, 1, 1, 1);
                }
            }
            else
            {
                itemCostText.text = $"{item.cost} GP";

                int currentGold = goldAmount;
                if (currentGold < 0 && onItemPurchased != null)
                {
                    var equipmentManager = FindAnyObjectByType<EquipmentManager>();
                    if (equipmentManager != null)
                        currentGold = equipmentManager.GetPlayerGold();
                }

                if (currentGold >= 0 && item.cost > currentGold)
                    itemCostText.style.color = Color.red;
                else
                    itemCostText.style.color = new Color(1, 1, 1, 1);
            }
        }

        // Update owned quantity
        if (ownedQuantityText != null)
            ownedQuantityText.text = $"{item.ownedQuantity}";

        // Check if this is a regular item or equipment
        bool isRegularItem = !isEquipmentTabSelected;

        // Always make sure the container is visible
        if (applicableContainer != null)
            ForceElementVisibility(applicableContainer, true);

        // Update the usable by / effect text
        if (itemDescText != null && usableByText != null)
        {
            if (isRegularItem)
            {
                ForceElementVisibility(itemDescText, true);
                ForceElementVisibility(usableByText, false);
                // For regular items, show the effect
                string effectText = GetItemEffect(item.name);
                itemDescText.text = $"Effect:\n{effectText}";
            }
            else
            {
                // For equipment, show usable characters
                ForceElementVisibility(itemDescText, false);
                ForceElementVisibility(usableByText, true);
            }
        }

        if (randiIcon != null)
            ForceElementVisibility(randiIcon, !isRegularItem);

        if (popoiIcon != null)
            ForceElementVisibility(popoiIcon, !isRegularItem);

        if (purimIcon != null)
            ForceElementVisibility(purimIcon, !isRegularItem);

        if (!isRegularItem)
            UpdateCharacterUsability(item);

        if (goldAmount >= 0)
            UpdateGoldDisplay(goldAmount);
    }

    private void UpdateGoldDisplay(int goldAmount)
    {
        if (goldText != null)
        {
            goldText.text = $"{goldAmount}";
        }
    }

    public void ShowNotEnoughGoldMessage()
    {
        whatWillYouBuyText.text = "You don't have\nenough GP!";
        showingInsufficientFunds = true;
        isConfirmingPurchase = false;

        var interactionManager = FindAnyObjectByType<InteractionUIManager>();
        if (interactionManager != null)
            interactionManager.SetButtonText("Ok", "", false);

        var cameraShaker = CameraShaker.Instance;
        if (cameraShaker != null)
            cameraShaker.ShakeCamera(0.5f, 0.05f);

        StartCoroutine(FlashCostText());
    }

    private IEnumerator FlashCostText()
    {
        if (itemCostText == null || currentSelectedItem == null)
            yield break;

        string costText = $"{currentSelectedItem.cost} GP";

        Color flashColor = new Color(1, 0, 0, 1); 

        Color returnColor;
        var equipmentManager = FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager != null && currentSelectedItem.cost > equipmentManager.GetPlayerGold())
            returnColor = new Color(0.8f, 0, 0, 1); // Darker red
        else
            returnColor = new Color(1, 1, 1, 1);

        for (int i = 0; i < 3; i++)
        {
            itemCostText.text = costText;
            itemCostText.style.color = flashColor;
            yield return new WaitForSeconds(0.2f);

            itemCostText.text = costText;
            itemCostText.style.color = returnColor;
            yield return new WaitForSeconds(0.2f);
        }

        itemCostText.text = costText;
        itemCostText.style.color = returnColor;

        ResetAllUI();
    }

    public void HideBuyMenu()
    {
        ForceElementVisibility(buyMenu, false);

        if (selectionBox != null)
            ForceElementVisibility(selectionBox, false);
    }

    public void HideAllMenus()
    {
        ForceElementVisibility(buyMenu, false);
    }

    private void SwitchTab(bool equipmentTab)
    {
        if (isEquipmentTabSelected != equipmentTab)
        {
            isEquipmentTabSelected = equipmentTab;
            UpdateTabVisuals();
            onTabChanged?.Invoke(equipmentTab);
        }
    }

    public void OnLeftRightKeyPressed(bool right)
    {
        if (IsInPurchaseState())
            return;

        if (showingPurchaseSuccess || showingInsufficientFunds || showingSellSuccess)
            return;

        if (currentItems == null || currentItems.Count == 0)
            return;

        int newIndex;

        if (right)
        {
            // Move anti-clockwise
            newIndex = (selectedItemIndex - 1 + currentItems.Count) % currentItems.Count;
        }
        else
        {
            // Move clockwise
            newIndex = (selectedItemIndex + 1) % currentItems.Count;
        }

        // Update selected index
        selectedItemIndex = newIndex;
        currentSelectedItem = currentItems[selectedItemIndex];

        // Reset UI text
        if (!showingPurchaseSuccess && !showingInsufficientFunds && !showingSellSuccess)
        {
            isConfirmingPurchase = false;
            isConfirmingSell = false;

            if (whatWillYouBuyText != null)
                whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
        }

        UpdateItemDetails(currentSelectedItem, -1);

        if (isInSellMode)
            UpdateCircularSellDisplay();
        else
            UpdateCircularDisplay();

        ResetAllUI();
        animationTime = 0f;
    }

    public void OnTabKeyPressed(bool toItemsTab)
    {
        if (IsInPurchaseState() || IsInSellState())
            return;

        if (showingPurchaseSuccess || showingInsufficientFunds || showingSellSuccess)
            return;

        if ((toItemsTab && !isEquipmentTabSelected) || (!toItemsTab && isEquipmentTabSelected))
            return;

        var equipmentManager = FindAnyObjectByType<EquipmentManager>();
        if (equipmentManager != null)
        {
            if (isInSellMode)
            {
                if (toItemsTab)
                {
                    List<EquipmentItem> ownedRegularItems = equipmentManager.GetRegularItems()
                        .Where(item => item.ownedQuantity > 0)
                        .ToList();

                    ShowSellRegularItems(ownedRegularItems);
                }
                else
                {
                    List<EquipmentItem> ownedEquipment = equipmentManager.GetEquipmentItems()
                        .Where(item => item.ownedQuantity > 0)
                        .ToList();

                    ShowSellEquipmentItems(ownedEquipment);
                }
            }
            else
            {
                if (toItemsTab)
                    ShowRegularItems(equipmentManager.GetRegularItems());
                else
                    ShowEquipmentItems(equipmentManager.GetEquipmentItems());
            }
        }
        else
        {
            Debug.LogError("EquipmentManager not found!");
        }
    }

    public void OnConfirmClicked()
    {
        // PART 1: HANDLE VARIOUS NOTIFICATION STATES FOR BOTH BUY AND SELL

        // Handle insufficient funds message (buying)
        if (showingInsufficientFunds)
        {
            // Reset text based on the current mode (buying or selling)
            whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
            showingInsufficientFunds = false;

            var interactionManager = FindAnyObjectByType<InteractionUIManager>();
            if (interactionManager != null)
            {
                // Set appropriate button text based on mode
                interactionManager.SetButtonText(isInSellMode ? "Sell" : "Buy", "Back", true);
            }

            ResetAllUI();
            return;
        }

        // Handle restricted item message (buying)
        if (showingRestrictedItem)
        {
            // Reset text based on the current mode
            whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
            showingRestrictedItem = false;

            var interactionManager = FindAnyObjectByType<InteractionUIManager>();
            if (interactionManager != null)
            {
                // Set appropriate button text based on mode
                interactionManager.SetButtonText(isInSellMode ? "Sell" : "Buy", "Back", true);
            }

            ResetAllUI();
            return;
        }

        // Handle success messages for both buying and selling
        if (showingPurchaseSuccess || showingSellSuccess)
        {
            // Reset text based on the current mode
            whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
            showingPurchaseSuccess = false;
            showingSellSuccess = false;

            ResetAllUI();

            // If this was a sell transaction and the item was completely sold out
            if (isInSellMode && currentSelectedItem != null && currentSelectedItem.ownedQuantity == 0)
            {
                // Remove the item from the list and update the display
                currentItems.RemoveAt(selectedItemIndex);

                // Adjust selectedItemIndex if needed
                if (selectedItemIndex >= currentItems.Count && currentItems.Count > 0)
                    selectedItemIndex = currentItems.Count - 1;

                // If we still have items, update the details for the new selected item
                if (currentItems.Count > 0)
                {
                    currentSelectedItem = currentItems[selectedItemIndex];
                    UpdateItemDetails(currentSelectedItem, -1);
                }
                else
                {
                    // No more items to sell in this category
                    if (whatWillYouBuyText != null)
                    {
                        whatWillYouBuyText.text = isEquipmentTabSelected ? "No equipment\nto sell!" : "No items\nto sell!";
                    }

                    // Hide item details UI
                    HideItemDetailsUI();
                }

                // Update circular display with the new item list
                UpdateCircularSellDisplay();
            }
            return;
        }

        // Guard clause for states where we should block further interaction
        if (showingInsufficientFunds || insufficientFunds || showingRestrictedItem)
            return;

        // PART 2: HANDLE INITIAL CONFIRM CLICK (FIRST CLICK) FOR BOTH BUY AND SELL

        if (!isConfirmingPurchase && !isConfirmingSell)
        {
            // Make sure we have valid selection
            if (currentItems != null && selectedItemIndex >= 0 && selectedItemIndex < currentItems.Count)
            {
                currentSelectedItem = currentItems[selectedItemIndex];

                if (isInSellMode)
                {
                    // SELLING FLOW - First confirmation click

                    // Check if this is a restricted item first
                    if (currentSelectedItem.cost == 0)
                    {
                        // Show error message for restricted items immediately
                        ShowRestrictedItemMessage();
                        return;
                    }

                    // Calculate the sell price (half of buy price)
                    int sellPrice = Mathf.FloorToInt(currentSelectedItem.cost / 2f);

                    // Show confirmation dialog
                    whatWillYouBuyText.text = $"I'll pay {sellPrice} GP\nfor it. Deal?";
                    isConfirmingSell = true;
                }
                else
                {
                    // BUYING FLOW - First confirmation click
                    if (currentSelectedItem.cost == 0)
                    {
                        ShowRestrictedItemMessage();
                        return;
                    }

                    whatWillYouBuyText.text = $"It's {currentSelectedItem.cost} GP,\nOkay?";
                    isConfirmingPurchase = true;
                }
            }
        }
        // PART 3: HANDLE SECOND CONFIRM CLICK (ACTUAL TRANSACTION) FOR BOTH BUY AND SELL
        else if (isConfirmingPurchase)
        {
            // BUYING FLOW - Second confirmation click
            if (currentSelectedItem != null)
            {
                bool success = onItemPurchased != null && onItemPurchased.Invoke(currentSelectedItem);

                if (success)
                {
                    whatWillYouBuyText.text = "Thank you!";
                    showingPurchaseSuccess = true;
                    isConfirmingPurchase = false;
                }
                else
                {
                    whatWillYouBuyText.text = "You don't have\nenough GP!";
                    showingInsufficientFunds = true;
                    isConfirmingPurchase = false;
                    ResetAllUI();
                }
            }
        }
        else if (isConfirmingSell)
        {
            // SELLING FLOW - Second confirmation click
            if (currentSelectedItem != null)
            {
                bool success = onItemSold != null && onItemSold.Invoke(currentSelectedItem);

                if (success)
                {
                    whatWillYouBuyText.text = "Thank you!";
                    showingSellSuccess = true;
                    isConfirmingSell = false;
                }
            }
        }
    }

    public void OnCancelClicked()
    {
        if (showingPurchaseSuccess)
        {
            whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
            showingPurchaseSuccess = false;
            ResetAllUI();
        }
        else if (showingSellSuccess)
        {
            whatWillYouBuyText.text = isInSellMode ? "What'll you be\nselling?" : "What'll you be\nbuying?";
            showingSellSuccess = false;
            ResetAllUI();
        }
        else if (isConfirmingPurchase)
        {
            whatWillYouBuyText.text = "What'll you be\nbuying?";
            isConfirmingPurchase = false;
            insufficientFunds = false;
            showingInsufficientFunds = false;
            showingRestrictedItem = false;
            ResetAllUI();
        }
        else if (isConfirmingSell)
        {
            whatWillYouBuyText.text = "What'll you be\nselling?";
            isConfirmingSell = false;
            ResetAllUI();
        }
        else
            onCancelled?.Invoke();
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

    private void ResetAllUI()
    {
        if (currentSelectedItem != null)
        {
            if (itemNameText != null)
                itemNameText.text = currentSelectedItem.name;

            if (itemCostText != null)
            {
                if (currentSelectedItem.cost == 0)
                {
                    itemCostText.text = "- GP";
                    itemCostText.style.color = new Color(1, 1, 1, 1);
                }
                else
                {
                    if (isInSellMode)
                    {
                        int sellPrice = Mathf.FloorToInt(currentSelectedItem.cost / 2f);
                        itemCostText.text = $"{sellPrice} GP";
                        itemCostText.style.color = new Color(1, 1, 1, 1);
                    }
                    else
                    {
                        itemCostText.text = $"{currentSelectedItem.cost} GP";

                        itemCostText.style.display = DisplayStyle.Flex;
                        itemCostText.style.visibility = Visibility.Visible;
                        itemCostText.style.opacity = 1;

                        var equipmentManager = FindAnyObjectByType<EquipmentManager>();
                        if (equipmentManager != null)
                        {
                            int playerGold = equipmentManager.GetPlayerGold();

                            if (currentSelectedItem.cost > playerGold)
                                itemCostText.style.color = Color.red;
                            else
                                itemCostText.style.color = new Color(1, 1, 1, 1);
                        }
                        else
                            itemCostText.style.color = new Color(1, 1, 1, 1);
                    }
                }
            }

            bool isRegularItem = !isEquipmentTabSelected;

            if (usableByText != null)
            {
                if (isRegularItem)
                {
                    // For regular items, show the effect description
                    string effectText = GetItemEffect(currentSelectedItem.name);
                    usableByText.text = $"Effect:\n{effectText}";
                }
                else
                {
                    // For equipment, show usable characters as before
                    usableByText.text = $"Usable\nBy:";
                }
            }

            if (ownedQuantityText != null)
                ownedQuantityText.text = $"{currentSelectedItem.ownedQuantity}";

            // Always show the container, but toggle character icons based on item type
            if (applicableContainer != null)
            {
                ForceElementVisibility(applicableContainer, true);

                // Toggle visibility of character icons based on item type
                if (randiIcon != null)
                    ForceElementVisibility(randiIcon, !isRegularItem);

                if (popoiIcon != null)
                    ForceElementVisibility(popoiIcon, !isRegularItem);

                if (purimIcon != null)
                    ForceElementVisibility(purimIcon, !isRegularItem);
            }

            // Only update character usability for equipment items
            if (!isRegularItem)
            {
                UpdateCharacterUsability(currentSelectedItem);
            }
        }

        if (buyMenu != null)
            buyMenu.MarkDirtyRepaint();
    }

    public void ShowEquipmentSellMenu(
    List<EquipmentItem> ownedEquipment,
    List<EquipmentItem> ownedRegularItems,
    int goldAmount,
    Func<EquipmentItem, bool> sellCallback,
    Action cancelCallback,
    Action<bool> tabChangeCallback)
    {
        isInSellMode = true;
        onItemPurchased = null;
        onItemSold = sellCallback;
        onCancelled = cancelCallback;
        onTabChanged = tabChangeCallback;

        // Show the menu UI
        ForceElementVisibility(buyMenu, true);

        // Make sure we have the UI elements
        if (equipmentUIElements.Count == 0 || regularItemUIElements.Count == 0)
            FindAllItemUIElements();

        // Hide all items initially
        HideAllItemElements();

        // Reset all states
        isConfirmingPurchase = false;
        isConfirmingSell = false;
        insufficientFunds = false;
        showingPurchaseSuccess = false;
        showingSellSuccess = false;
        showingInsufficientFunds = false;
        showingRestrictedItem = false;

        // Set up the UI text
        if (whatWillYouBuyText != null)
            whatWillYouBuyText.text = "What'll you be\nselling?";

        // Update gold display
        UpdateGoldDisplay(goldAmount);

        // Select the current items list based on the tab
        currentItems = isEquipmentTabSelected ? ownedEquipment : ownedRegularItems;

        // Show tabs if we have any items
        if (equipmentTab != null && itemTab != null)
        {
            ForceElementVisibility(equipmentTab, true);
            ForceElementVisibility(itemTab, true);
            UpdateTabVisuals();
        }

        // Check if we have items to sell
        if (currentItems == null || currentItems.Count == 0)
        {
            // No items to sell
            whatWillYouBuyText.text = isEquipmentTabSelected ? "No equipment\nto sell!" : "No items\nto sell!";

            HideItemDetailsUI();
            if (selectionBox != null)
                ForceElementVisibility(selectionBox, false);
            return;
        }

        // Reset selection
        selectedItemIndex = 0;
        rotationOffset = 0;

        // Update the display with owned items
        UpdateCircularSellDisplay();

        // Update item details for the first item
        if (currentItems.Count > 0)
        {
            UpdateItemDetails(currentItems[0], goldAmount);
        }
    }

    public void ShowSellEquipmentItems(List<EquipmentItem> ownedEquipment)
    {
        isEquipmentTabSelected = true;
        currentItems = ownedEquipment;

        // Update tab visuals
        UpdateTabVisuals();

        HideAllItemElements();

        // Reset selection index if we have items
        if (currentItems.Count > 0)
        {
            selectedItemIndex = 0;

            if (whatWillYouBuyText != null)
                whatWillYouBuyText.text = "What'll you be\nselling?";

            // Update the circular display with owned items
            UpdateCircularSellDisplay();

            // Update item details
            UpdateItemDetails(currentItems[0], -1);
        }
        else
        {
            // No equipment to sell
            if (selectionBox != null)
                ForceElementVisibility(selectionBox, false);

            HideItemDetailsUI();
            whatWillYouBuyText.text = "No equipment\nto sell!";
        }
    }

    public void ShowSellRegularItems(List<EquipmentItem> ownedRegularItems)
    {
        isEquipmentTabSelected = false;
        currentItems = ownedRegularItems;

        // Update tab visuals
        UpdateTabVisuals();

        // Hide all items first
        HideAllItemElements();

        // Reset selection index if we have items
        if (currentItems.Count > 0)
        {
            selectedItemIndex = 0;

            if (whatWillYouBuyText != null)
                whatWillYouBuyText.text = "What'll you be\nselling?";

            // Update the circular display with owned items
            UpdateCircularSellDisplay();

            // Update item details
            UpdateItemDetails(currentItems[0], -1);
        }
        else
        {
            // No items to sell
            if (selectionBox != null)
                ForceElementVisibility(selectionBox, false);

            HideItemDetailsUI();
            whatWillYouBuyText.text = "No items\nto sell!";
        }
    }

    private void UpdateCircularSellDisplay()
    {
        // First, hide all item elements
        HideAllItemElements();

        // Early exit if we have no items
        if (currentItems == null || currentItems.Count == 0)
        {
            if (selectionBox != null)
                ForceElementVisibility(selectionBox, false);

            if (isInSellMode)
                HideItemDetailsUI();

            return;
        }

        // Show item details UI
        ShowItemDetailsUI();

        // Get the source collection based on the current tab
        List<VisualElement> sourceCollection = isEquipmentTabSelected ?
            equipmentUIElements : regularItemUIElements;

        // Create a list to hold elements that match our owned items
        List<VisualElement> itemElementsToShow = new List<VisualElement>();

        // Find the UI element for each owned item
        foreach (var item in currentItems)
        {
            // Find the UI element that matches this item
            VisualElement matchingElement = null;

            foreach (var element in sourceCollection)
            {
                // Compare element name with item name (remove spaces from item name)
                string normalizedItemName = item.name.Replace(" ", "");
                if (element.name.Contains(normalizedItemName))
                {
                    matchingElement = element;
                    break;
                }
            }

            if (matchingElement != null)
            {
                itemElementsToShow.Add(matchingElement);
            }
            else
            {
                Debug.LogWarning($"Could not find UI element for item: {item.name}");
            }
        }

        // If we have no elements to show, exit
        if (itemElementsToShow.Count == 0)
        {
            if (selectionBox != null)
                ForceElementVisibility(selectionBox, false);
            return;
        }

        // Recalculate positions based on the number of items to show
        CalculateCircularPositions(itemElementsToShow.Count);

        // Update the activeItemUIElements list to match what we're displaying
        // This is critical for the selection box animation to work correctly
        activeItemUIElements = itemElementsToShow;

        // Position each element in the circle
        for (int i = 0; i < itemElementsToShow.Count; i++)
        {
            int positionIndex = (i - selectedItemIndex + circularPositions.Count) % circularPositions.Count;
            Vector2 position = circularPositions[positionIndex];
            VisualElement element = itemElementsToShow[i];

            bool isSelected = i == selectedItemIndex;

            // Show the element
            ForceElementVisibility(element, true);

            // Set size based on selection state
            float size = isSelected ? 96 : 64;
            element.style.width = size;
            element.style.height = size;

            // Apply tint based on selection
            element.style.unityBackgroundImageTintColor = isSelected ? normalTint : nonSelectedTint;

            // Position the element
            element.style.position = Position.Absolute;
            element.style.left = position.x - size / 2;
            element.style.top = position.y - size / 2;
        }

        // Update selection box visibility
        if (selectionBox != null)
        {
            if (selectedItemIndex >= 0 && selectedItemIndex < itemElementsToShow.Count)
                ForceElementVisibility(selectionBox, true);
            else
                ForceElementVisibility(selectionBox, false);
        }
    }

    public void HideSellMenu()
    {
        ForceElementVisibility(buyMenu, false);

        if (selectionBox != null)
            ForceElementVisibility(selectionBox, false);
    }

    private void HideItemDetailsUI()
    {
        if (itemNameText != null)
            ForceElementVisibility(itemNameText, false);

        if (itemCostText != null)
            ForceElementVisibility(itemCostText, false);

        if (usableByText != null)
            ForceElementVisibility(usableByText, false);
        
        if (ownededText != null)
            ForceElementVisibility(ownededText, false);

        if (ownedQuantityText != null)
            ForceElementVisibility(ownedQuantityText, false);

        if (randiIcon != null)
            ForceElementVisibility(randiIcon, false);

        if (popoiIcon != null)
            ForceElementVisibility(popoiIcon, false);

        if (purimIcon != null)
            ForceElementVisibility(purimIcon, false);
    }

    public void ShowItemDetailsUI()
    {
        if (itemNameText != null)
            ForceElementVisibility(itemNameText, true);

        if (itemCostText != null)
            ForceElementVisibility(itemCostText, true);

        if (usableByText != null)
            ForceElementVisibility(usableByText, true);

        if (ownededText != null)
            ForceElementVisibility(ownededText, true);

        if (ownedQuantityText != null)
            ForceElementVisibility(ownedQuantityText, true);

        if (applicableContainer != null)
            ForceElementVisibility(applicableContainer, true);

        // Check if we're showing equipment or regular items
        bool isRegularItem = !isEquipmentTabSelected;

        // Show/hide character icons based on item type
        if (randiIcon != null)
            ForceElementVisibility(randiIcon, !isRegularItem);

        if (popoiIcon != null)
            ForceElementVisibility(popoiIcon, !isRegularItem);

        if (purimIcon != null)
            ForceElementVisibility(purimIcon, !isRegularItem);
    }

    public void ShowRestrictedItemMessage()
    {
        whatWillYouBuyText.text = "Oops! This is a\nrestricted Item!";
        showingRestrictedItem = true;
        isConfirmingSell = false;

        var interactionManager = FindAnyObjectByType<InteractionUIManager>();
        if (interactionManager != null)
            interactionManager.SetButtonText("Ok", "", false);

        var cameraShaker = CameraShaker.Instance;
        if (cameraShaker != null)
            cameraShaker.ShakeCamera(0.3f, 0.04f);
    }

    private string GetItemEffect(string itemName)
    {
        switch (itemName)
        {
            case "Candy":
                return "Recovers 100 HP";
            case "Chocolate":
                return "Recovers 250 HP";
            case "Royal Jam":
                return "Recovers full HP";
            case "Faerie Walnut":
                return "Recovers 50 MP";
            case "Medical Herb":
                return "Heals status ailments";
            case "Cup of Wishes":
                return "Saves a ghosted player";
            case "Barrel":
                return "Protects player";
            case "Flammie Drum":
                return "Summons Flammie";
            case "Magic Rope":
                return "Teleports to the entrance";
            case "Moogle Belt":
                return "Restores moogled player";
            case "Midge Mallet":
                return "Restores pygmized player";
            default:
                return "No effect";
        }
    }
}