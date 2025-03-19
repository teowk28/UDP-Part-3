using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class EquipmentItem
{
    public string name;
    public int cost;
    public string usableBy = "";
    public int ownedQuantity;
    public Sprite icon;

    public bool usableByRandi = true;
    public bool usableByPopoi = true;
    public bool usableByPurim = true;

    public bool IsUsableBy(CharacterType character)
    {
        switch (character)
        {
            case CharacterType.Randi:
                return usableByRandi;
            case CharacterType.Popoi:
                return usableByPopoi;
            case CharacterType.Purim:
                return usableByPurim;
            default:
                return false;
        }
    }

    public string GetUsabilityString()
    {
        if (usableByRandi && usableByPopoi && usableByPurim)
            return "All";

        List<string> users = new List<string>();
        if (usableByRandi) users.Add("Randi");
        if (usableByPopoi) users.Add("Popoi");
        if (usableByPurim) users.Add("Purim");

        return string.Join(", ", users);
    }
}

public enum CharacterType
{
    Randi,
    Popoi,
    Purim
}

public class EquipmentManager : MonoBehaviour
{
    [SerializeField] private List<EquipmentItem> equipmentItems = new List<EquipmentItem>();
    [SerializeField] private List<EquipmentItem> regularItems = new List<EquipmentItem>();

    [SerializeField] private List<Sprite> equipmentItemsIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> regularItemsIcons = new List<Sprite>();

    private int playerGold = 10000; // Starting gold

    public List<EquipmentItem> GetEquipmentItems() => equipmentItems;
    public List<EquipmentItem> GetRegularItems() => regularItems;
    public int GetPlayerGold() => playerGold;

    public List<EquipmentItem> GetBuyableEquipmentItems()
    {
        return equipmentItems.Where(item => item.cost > 0).ToList();
    }

    public List<EquipmentItem> GetBuyableRegularItems()
    {
        return regularItems.Where(item => item.cost > 0).ToList();
    }
    private void InitializeSampleData()
    {
        equipmentItems.Clear();
        equipmentItems.Add(new EquipmentItem { name = "Tiger Suit", cost = 6375, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = false });
        equipmentItems.Add(new EquipmentItem { name = "Fancy Overalls", cost = 675, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Kung Fu Suit", cost = 25, ownedQuantity = 0, usableByRandi = false, usableByPopoi = false, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Midge Robe", cost = 800, ownedQuantity = 0, usableByRandi = false, usableByPopoi = true, usableByPurim = false });
        equipmentItems.Add(new EquipmentItem { name = "Lazuri Ring", cost = 8800, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Dragon Helm", cost = 7500, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Rabite Cap", cost = 45, ownedQuantity = 998, usableByRandi = false, usableByPopoi = true, usableByPurim = false });
        equipmentItems.Add(new EquipmentItem { name = "Quill Cap", cost = 110, ownedQuantity = 0, usableByRandi = false, usableByPopoi = true, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Raccoon Cap", cost = 550, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Gauntlet", cost = 37500, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Wristband", cost = 45, ownedQuantity = 0, usableByRandi = true, usableByPopoi = false, usableByPurim = true });
        equipmentItems.Add(new EquipmentItem { name = "Elbow Pad", cost = 90, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });

        regularItems.Clear();
        regularItems.Add(new EquipmentItem { name = "Barrel", cost = 900, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Candy", cost = 10, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Chocolate", cost = 30, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Cup of Wishes", cost = 150, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Faerie Walnut", cost = 1000, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Flammie Drum", cost = 0, ownedQuantity = 1, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Magic Rope", cost = 0, ownedQuantity = 1, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Medical Herb", cost = 10, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Midge Mallet", cost = 0, ownedQuantity = 1, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Moogle Belt", cost = 0, ownedQuantity = 1, usableByRandi = true, usableByPopoi = true, usableByPurim = true });
        regularItems.Add(new EquipmentItem { name = "Royal Jam", cost = 350, ownedQuantity = 0, usableByRandi = true, usableByPopoi = true, usableByPurim = true });

        foreach (var item in equipmentItems.Concat(regularItems))
        {
            item.usableBy = item.GetUsabilityString();
        }

        //set icon to preset constructor stuff cus why wasn't the equipment done in editor :'(
        for (int i = 0; i < equipmentItems.Count; ++i) {
            if(i < equipmentItemsIcons.Count)
                equipmentItems[i].icon = equipmentItemsIcons[i];
        }
        
        for (int i = 0; i < regularItems.Count; ++i) {
            if(i < regularItemsIcons.Count)
                regularItems[i].icon = regularItemsIcons[i];
        }
    }

    private void Start()
    {
        InitializeSampleData();
    }

    public bool PurchaseItem(EquipmentItem item)
    {
        if (playerGold >= item.cost)
        {
            playerGold -= item.cost;
            item.ownedQuantity++;
            Debug.Log($"Purchased {item.name} for {item.cost} GP. {playerGold} GP remaining.");
            return true;
        }

        Debug.Log($"Not enough gold to purchase {item.name}. Need {item.cost} GP, have {playerGold} GP.");
        return false;
    }

    public bool SellItem(EquipmentItem item)
    {
        if (item.ownedQuantity > 0)
        {
            int sellPrice = Mathf.FloorToInt(item.cost / 2f); // Half price when selling
            playerGold += sellPrice;
            item.ownedQuantity--;
            Debug.Log($"Sold {item.name} for {sellPrice} GP. Now have {playerGold} GP.");
            return true;
        }

        Debug.Log($"Cannot sell {item.name}. No items owned.");
        return false;
    }
}