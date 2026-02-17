using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WandPowerUp : MonoBehaviour
{
    [Header("Power-Up Configuration")]
    [Tooltip("Number of balanced item swaps to perform (each swap exchanges 2 items of different types)")]
    [SerializeField] private int swapCount = 2;
    
    [Tooltip("Chance (0-1) to prioritize slots with 2 matching items")]
    [Range(0f, 1f)]
    [SerializeField] private float prioritizeNearMatchesChance = 0.8f;
    
    [Tooltip("Visual feedback when activated")]
    [SerializeField] private ParticleSystem activationVfx;
    
    [Header("References")]
    [Tooltip("Reference to BoardBootstrap to access item prefabs")]
    [SerializeField] private BoardBootstrap boardBootstrap;
    
    [Tooltip("Cooldown duration in seconds")]
    [SerializeField] private float cooldownDuration = 15f;
    
    private Button powerUpButton;
    private float cooldownRemaining = 0f;
    private Dictionary<ItemType, List<ItemView>> itemsByType = new Dictionary<ItemType, List<ItemView>>();
    
    private void Awake()
    {
        powerUpButton = GetComponent<Button>();
        powerUpButton.onClick.AddListener(ActivateWand);
        UpdateButtonInteractable();
    }
    
    private void Update()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            UpdateButtonInteractable();
        }
    }
    
    private void ActivateWand()
    {
        if (cooldownRemaining > 0f) return;
        
        if (!CatalogItemsByType()) return;
        
        if (!ValidateItemCountBalance()) return;
        
        int successfulSwaps = PerformBalancedSwaps();
        
        if (successfulSwaps > 0)
        {
            if (activationVfx != null)
            {
                activationVfx.transform.position = transform.position;
                activationVfx.Play();
            }
            
            cooldownRemaining = cooldownDuration;
            UpdateButtonInteractable();
        }
    }
    
    private bool CatalogItemsByType()
    {
        itemsByType.Clear();
        
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            itemsByType[type] = new List<ItemView>();
        }
        
        var allItems = FindObjectsOfType<ItemView>(true);
        foreach (var item in allItems)
        {
            if (item != null && item.gameObject.activeInHierarchy)
            {
                if (itemsByType.ContainsKey(item.Type))
                {
                    itemsByType[item.Type].Add(item);
                }
            }
        }
        
        return true;
    }
    
    private bool ValidateItemCountBalance()
    {
        foreach (var kvp in itemsByType)
        {
            if (kvp.Value.Count % 3 != 0)
            {
                return false;
            }
        }
        return true;
    }
    
    private int PerformBalancedSwaps()
    {
        int swapsPerformed = 0;
        
        var slots = FindObjectsOfType<SlotView>(true);
        var nearMatchSlots = FindNearMatchSlots(slots);
        
        for (int i = 0; i < swapCount && swapsPerformed < swapCount; i++)
        {
            ItemView itemA, itemB;
            if (Random.value < prioritizeNearMatchesChance && nearMatchSlots.Count > 0)
            {
                if (TryFindSwapPairInNearMatches(nearMatchSlots, out itemA, out itemB))
                {
                    ExecuteBalancedSwap(itemA, itemB);
                    swapsPerformed++;
                    continue;
                }
            }
            
            if (TryFindRandomBalancedSwap(out itemA, out itemB))
            {
                ExecuteBalancedSwap(itemA, itemB);
                swapsPerformed++;
            }
        }
        
        foreach (var slot in slots)
        {
            slot.SyncItemsFromCells();
            slot.ArrangeItems();
        }
        
        return swapsPerformed;
    }
    
    private List<SlotView> FindNearMatchSlots(SlotView[] allSlots)
    {
        var nearMatchSlots = new List<SlotView>();
        
        foreach (var slot in allSlots)
        {
            slot.SyncItemsFromCells();
            var items = new List<ItemView>(slot.GetComponentsInChildren<ItemView>(true));
            
            if (items.Count == 3)
            {
                var typeCounts = new Dictionary<ItemType, int>();
                foreach (var item in items)
                {
                    if (!typeCounts.ContainsKey(item.Type))
                        typeCounts[item.Type] = 0;
                    typeCounts[item.Type]++;
                }
                
                bool hasPair = false;
                bool hasSingle = false;
                foreach (var count in typeCounts.Values)
                {
                    if (count == 2) hasPair = true;
                    if (count == 1) hasSingle = true;
                }
                
                if (hasPair && hasSingle)
                {
                    nearMatchSlots.Add(slot);
                }
            }
        }
        
        return nearMatchSlots;
    }
    
    private bool TryFindSwapPairInNearMatches(List<SlotView> nearMatchSlots, out ItemView itemA, out ItemView itemB)
    {
        itemA = null;
        itemB = null;
        
        ShuffleList(nearMatchSlots);
        
        foreach (var slot in nearMatchSlots)
        {
            slot.SyncItemsFromCells();
            var items = new List<ItemView>(slot.GetComponentsInChildren<ItemView>(true));
            
            if (items.Count != 3) continue;
            
            var typeCounts = new Dictionary<ItemType, int>();
            foreach (var item in items)
            {
                if (!typeCounts.ContainsKey(item.Type))
                    typeCounts[item.Type] = 0;
                typeCounts[item.Type]++;
            }
            
            ItemType oddType = default;
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value == 1)
                {
                    oddType = kvp.Key;
                    break;
                }
            }
            
            var oddItem = items.Find(i => i.Type == oddType);
            if (oddItem == null) continue;
            
            foreach (var otherType in itemsByType.Keys)
            {
                if (otherType == oddType) continue;
                
                foreach (var candidate in itemsByType[otherType])
                {
                    if (candidate == null || candidate == oddItem) continue;
                    
                    var candidateSlot = candidate.GetComponentInParent<SlotView>();
                    if (candidateSlot == null || candidateSlot == slot) continue;
                    
                    if (WouldImproveMatch(slot, oddItem.Type, candidate.Type) &&
                        WouldImproveMatch(candidateSlot, candidate.Type, oddItem.Type))
                    {
                        itemA = oddItem;
                        itemB = candidate;
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    private bool WouldImproveMatch(SlotView slot, ItemType currentType, ItemType targetType)
    {
        slot.SyncItemsFromCells();
        var items = new List<ItemView>(slot.GetComponentsInChildren<ItemView>(true));
        
        int currentMatches = CountMatchingItems(items, currentType);
        int targetMatches = CountMatchingItems(items, targetType);
        
        return (targetMatches + 1) > currentMatches;
    }
    
    private int CountMatchingItems(List<ItemView> items, ItemType type)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item.Type == type) count++;
        }
        return count;
    }
    
    private bool TryFindRandomBalancedSwap(out ItemView itemA, out ItemView itemB)
    {
        itemA = null;
        itemB = null;
        
        var availableTypes = new List<ItemType>();
        foreach (var kvp in itemsByType)
        {
            if (kvp.Value.Count > 0)
                availableTypes.Add(kvp.Key);
        }
        
        if (availableTypes.Count < 2) return false;
        
        ShuffleList(availableTypes);
        
        for (int i = 0; i < availableTypes.Count - 1; i++)
        {
            var typeA = availableTypes[i];
            var typeB = availableTypes[i + 1];
            
            if (itemsByType[typeA].Count > 0 && itemsByType[typeB].Count > 0)
            {
                ShuffleList(itemsByType[typeA]);
                ShuffleList(itemsByType[typeB]);
                
                itemA = itemsByType[typeA][0];
                itemB = itemsByType[typeB][0];
                
                var slotA = itemA.GetComponentInParent<SlotView>();
                var slotB = itemB.GetComponentInParent<SlotView>();
                
                if (slotA != null && slotB != null && slotA != slotB)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void ExecuteBalancedSwap(ItemView itemA, ItemView itemB)
    {
        if (itemA == null || itemB == null) return;
        
        var cellA = itemA.transform.parent;
        var cellB = itemB.transform.parent;
        var slotA = cellA.GetComponentInParent<SlotView>();
        var slotB = cellB.GetComponentInParent<SlotView>();
        
        if (slotA != null) slotA.RemoveItem(itemA);
        if (slotB != null) slotB.RemoveItem(itemB);
        
        var prefabA = GetPrefabForType(itemB.Type);
        var prefabB = GetPrefabForType(itemA.Type);
        
        if (prefabA == null || prefabB == null) return;
        
        Destroy(itemA.gameObject);
        Destroy(itemB.gameObject);
        
        var newItemA = Instantiate(prefabA, cellA, false);
        var newItemB = Instantiate(prefabB, cellB, false);
        
        FixItemTransform(newItemA);
        FixItemTransform(newItemB);
        
        var itemViewA = newItemA.GetComponent<ItemView>();
        var itemViewB = newItemB.GetComponent<ItemView>();
        
        if (slotA != null && itemViewA != null)
        {
            slotA.TryAddItemToCell(itemViewA, cellA as RectTransform);
        }
        
        if (slotB != null && itemViewB != null)
        {
            slotB.TryAddItemToCell(itemViewB, cellB as RectTransform);
        }
    }
    
    private GameObject GetPrefabForType(ItemType type)
    {
        if (boardBootstrap == null)
        {
            boardBootstrap = FindObjectOfType<BoardBootstrap>();
            if (boardBootstrap == null) return null;
        }
        
        var field = typeof(BoardBootstrap).GetField("itemPrefabs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            var prefabs = field.GetValue(boardBootstrap) as List<GameObject>;
            if (prefabs != null)
            {
                foreach (var prefab in prefabs)
                {
                    if (prefab != null)
                    {
                        var itemView = prefab.GetComponent<ItemView>();
                        if (itemView != null && itemView.Type == type)
                        {
                            return prefab;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    private void FixItemTransform(GameObject itemGo)
    {
        var rt = itemGo.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
    
    private void UpdateButtonInteractable()
    {
        if (powerUpButton != null)
            powerUpButton.interactable = (cooldownRemaining <= 0f);
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}