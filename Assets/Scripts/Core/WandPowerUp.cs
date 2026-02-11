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
        if (cooldownRemaining > 0f)
        {
            Debug.LogWarning($"[WandPowerUp] On cooldown for {cooldownRemaining:F1}s");
            return;
        }
        
        // 1. Catalog all items by type (critical for solvability)
        if (!CatalogItemsByType())
        {
            Debug.LogError("[WandPowerUp] Failed to catalog items - cannot guarantee solvability");
            return;
        }
        
        // 2. Verify solvability precondition (all counts must be multiples of 3)
        if (!ValidateItemCountBalance())
        {
            Debug.LogError("[WandPowerUp] Board state is unbalanced - cannot safely activate wand");
            return;
        }
        
        // 3. Perform balanced swaps that preserve type counts
        int successfulSwaps = PerformBalancedSwaps();
        
        if (successfulSwaps > 0)
        {
            // Visual feedback
            if (activationVfx != null)
            {
                activationVfx.transform.position = transform.position;
                activationVfx.Play();
            }
            
            // Cooldown
            cooldownRemaining = cooldownDuration;
            UpdateButtonInteractable();
            
            Debug.Log($"[WandPowerUp] Successfully performed {successfulSwaps} balanced swaps. Board remains solvable!");
        }
        else
        {
            Debug.LogWarning("[WandPowerUp] No valid swaps found. Board state unchanged.");
        }
    }
    
    private bool CatalogItemsByType()
    {
        itemsByType.Clear();
        
        // Initialize dictionaries for all possible types
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            itemsByType[type] = new List<ItemView>();
        }
        
        // Collect all items on board
        var allItems = FindObjectsOfType<ItemView>(true);
        foreach (var item in allItems)
        {
            if (item != null && item.gameObject.activeInHierarchy)
            {
                if (itemsByType.ContainsKey(item.Type))
                {
                    itemsByType[item.Type].Add(item);
                }
                else
                {
                    Debug.LogWarning($"[WandPowerUp] Unknown item type: {item.Type}");
                }
            }
        }
        
        return true;
    }
    
    private bool ValidateItemCountBalance()
    {
        // Solvability requirement: each type must have count % 3 == 0
        foreach (var kvp in itemsByType)
        {
            if (kvp.Value.Count % 3 != 0)
            {
                Debug.LogError($"[WandPowerUp] Type {kvp.Key} has {kvp.Value.Count} items (not divisible by 3). Board is unsolvable!");
                return false;
            }
        }
        return true;
    }
    
    private int PerformBalancedSwaps()
    {
        int swapsPerformed = 0;
        
        // Get all slots for strategic selection
        var slots = FindObjectsOfType<SlotView>(true);
        var nearMatchSlots = FindNearMatchSlots(slots);
        
        // Attempt swaps up to requested count
        for (int i = 0; i < swapCount && swapsPerformed < swapCount; i++)
        {
            // 70% chance to target near-match slots for strategic improvement
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
            
            // Fallback: random balanced swap anywhere on board
            if (TryFindRandomBalancedSwap(out itemA, out itemB))
            {
                ExecuteBalancedSwap(itemA, itemB);
                swapsPerformed++;
            }
        }
        
        // Refresh all slots after swaps
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
            
            // Slot has exactly 2 items of same type + 1 different = near match
            if (items.Count == 3)
            {
                var typeCounts = new Dictionary<ItemType, int>();
                foreach (var item in items)
                {
                    if (!typeCounts.ContainsKey(item.Type))
                        typeCounts[item.Type] = 0;
                    typeCounts[item.Type]++;
                }
                
                // Check for 2-1 distribution
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
        
        // Shuffle to avoid bias
        ShuffleList(nearMatchSlots);
        
        foreach (var slot in nearMatchSlots)
        {
            slot.SyncItemsFromCells();
            var items = new List<ItemView>(slot.GetComponentsInChildren<ItemView>(true));
            
            if (items.Count != 3) continue;
            
            // Find the "odd one out" (single item type)
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
            
            // Find the odd item in this slot
            var oddItem = items.Find(i => i.Type == oddType);
            if (oddItem == null) continue;
            
            // Find a matching item of oddType in ANOTHER slot to swap with
            foreach (var otherType in itemsByType.Keys)
            {
                if (otherType == oddType) continue;
                
                // Look for an item of otherType that's in a slot with 2+ of its own type
                foreach (var candidate in itemsByType[otherType])
                {
                    if (candidate == null || candidate == oddItem) continue;
                    
                    var candidateSlot = candidate.GetComponentInParent<SlotView>();
                    if (candidateSlot == null || candidateSlot == slot) continue;
                    
                    // Verify this swap would improve the situation
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
        
        // Count current matches
        int currentMatches = CountMatchingItems(items, currentType);
        int targetMatches = CountMatchingItems(items, targetType);
        
        // Would swapping to targetType create a better situation?
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
        
        // Find two different types that have items available
        var availableTypes = new List<ItemType>();
        foreach (var kvp in itemsByType)
        {
            if (kvp.Value.Count > 0)
                availableTypes.Add(kvp.Key);
        }
        
        if (availableTypes.Count < 2) return false;
        
        // Shuffle types to avoid bias
        ShuffleList(availableTypes);
        
        // Try to find two items of different types
        for (int i = 0; i < availableTypes.Count - 1; i++)
        {
            var typeA = availableTypes[i];
            var typeB = availableTypes[i + 1];
            
            if (itemsByType[typeA].Count > 0 && itemsByType[typeB].Count > 0)
            {
                // Shuffle candidates
                ShuffleList(itemsByType[typeA]);
                ShuffleList(itemsByType[typeB]);
                
                itemA = itemsByType[typeA][0];
                itemB = itemsByType[typeB][0];
                
                // Ensure they're in different slots (prevent pointless swaps)
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
        
        // Get parent cells BEFORE destroying items
        var cellA = itemA.transform.parent;
        var cellB = itemB.transform.parent;
        var slotA = cellA.GetComponentInParent<SlotView>();
        var slotB = cellB.GetComponentInParent<SlotView>();
        
        // Remove from slots first
        if (slotA != null) slotA.RemoveItem(itemA);
        if (slotB != null) slotB.RemoveItem(itemB);
        
        // Get target prefabs (opposite types)
        var prefabA = GetPrefabForType(itemB.Type); // itemA gets itemB's type
        var prefabB = GetPrefabForType(itemA.Type); // itemB gets itemA's type
        
        if (prefabA == null || prefabB == null)
        {
            Debug.LogError("[WandPowerUp] Missing prefab for swap - aborting");
            return;
        }
        
        // Destroy old items
        Destroy(itemA.gameObject);
        Destroy(itemB.gameObject);
        
        // Instantiate new prefabs in opposite cells
        var newItemA = Instantiate(prefabA, cellA, false);
        var newItemB = Instantiate(prefabB, cellB, false);
        
        // Fix transforms
        FixItemTransform(newItemA);
        FixItemTransform(newItemB);
        
        // Re-register with slots
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
        
        Debug.Log($"[WandPowerUp] Swapped {itemA.Type} ↔ {itemB.Type} (preserved type counts)");
    }
    
    private GameObject GetPrefabForType(ItemType type)
    {
        if (boardBootstrap == null)
        {
            boardBootstrap = FindObjectOfType<BoardBootstrap>();
            if (boardBootstrap == null)
            {
                Debug.LogError("[WandPowerUp] BoardBootstrap not found!");
                return null;
            }
        }
        
        // Access private field via reflection since GetItemPrefabs() doesn't exist yet
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
        
        Debug.LogWarning($"[WandPowerUp] No prefab found for type {type}");
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
        {
            powerUpButton.interactable = (cooldownRemaining <= 0f);
            
            var textComponent = GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                if (cooldownRemaining > 0f)
                {
                    textComponent.text = $"Wand\n{cooldownRemaining:F0}s";
                }
                else
                {
                    textComponent.text = "Wand";
                }
            }
        }
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