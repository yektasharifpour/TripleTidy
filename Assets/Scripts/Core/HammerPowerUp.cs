using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HammerPowerUp : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Maximum number of times this power-up can be used per game")]
    [SerializeField] private int maxUses = 3;
    
    [Tooltip("Cooldown in seconds between uses")]
    [SerializeField] private float cooldownSeconds = 1f;
    
    [Tooltip("Optional visual effect when hammer completes a match")]
    [SerializeField] private ParticleSystem hammerEffect;
    
    [Tooltip("Optional sound to play when match is completed")]
    [SerializeField] private AudioClip matchSound;

    private Button button;
    private int remainingUses;
    private float lastUsedTime = -999f;
    private AudioSource audioSource;
    private Text buttonText;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>(true);
        
        remainingUses = maxUses;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && matchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        UpdateButtonState();
    }

    private void Start()
    {
        button.onClick.AddListener(ExecuteHammer);
    }

    private void Update()
    {
        // ALWAYS keep button state updated with current game state
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        bool onCooldown = Time.time - lastUsedTime < cooldownSeconds;
        bool hasPossibleMatch = HasAvailableMatch();
        button.interactable = remainingUses > 0 && !onCooldown && hasPossibleMatch;

        // Update button text
        if (buttonText != null)
        {
            if (remainingUses <= 0)
                buttonText.text = "No Uses Left";
            else if (onCooldown)
            {
                float remainingCooldown = Mathf.CeilToInt(cooldownSeconds - (Time.time - lastUsedTime));
                buttonText.text = $"Wait {remainingCooldown}s";
            }
            else if (!hasPossibleMatch)
                buttonText.text = "No Match";
            else
                buttonText.text = $"Hammer ({remainingUses})";
        }
    }

    private bool HasAvailableMatch()
    {
        var slots = FindObjectsOfType<SlotView>(true);
        if (slots == null || slots.Length == 0) return false;

        // Look for ANY slot with at least 2 matching items of the same type
        foreach (var slot in slots)
        {
            slot.SyncItemsFromCells();
            var items = slot.GetComponentsInChildren<ItemView>(true);
            
            if (items.Length < 2) continue; // Need at least 2 items to have a pair
            
            // Count item types in this slot
            var typeCounts = new Dictionary<ItemType, int>();
            foreach (var item in items)
            {
                if (item != null)
                {
                    if (!typeCounts.ContainsKey(item.Type))
                        typeCounts[item.Type] = 0;
                    typeCounts[item.Type]++;
                }
            }
            
            // Check if any type appears at least twice
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value >= 2)
                {
                    // Verify there's a matching item in ANOTHER slot to complete the match
                    if (HasMatchingItemInOtherSlot(kvp.Key, slot))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool HasMatchingItemInOtherSlot(ItemType itemType, SlotView excludeSlot)
    {
        var slots = FindObjectsOfType<SlotView>(true);
        foreach (var slot in slots)
        {
            if (slot == excludeSlot) continue;
            
            slot.SyncItemsFromCells();
            var items = slot.GetComponentsInChildren<ItemView>(true);
            
            foreach (var item in items)
            {
                if (item != null && item.Type == itemType)
                    return true;
            }
        }
        return false;
    }

    public void ExecuteHammer()
    {
        // Safety checks
        if (remainingUses <= 0) return;
        if (Time.time - lastUsedTime < cooldownSeconds) return;
        if (!HasAvailableMatch()) return;

        var slots = FindObjectsOfType<SlotView>(true);
        if (slots == null || slots.Length == 0) return;

        // Find first completable match opportunity
        foreach (var targetSlot in slots)
        {
            targetSlot.SyncItemsFromCells();
            var targetItems = targetSlot.GetComponentsInChildren<ItemView>(true);
            
            if (targetItems.Length < 2) continue;
            
            // Count item types in target slot
            var typeCounts = new Dictionary<ItemType, int>();
            foreach (var item in targetItems)
            {
                if (item != null)
                {
                    if (!typeCounts.ContainsKey(item.Type))
                        typeCounts[item.Type] = 0;
                    typeCounts[item.Type]++;
                }
            }
            
            // Find a type that appears at least twice
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value >= 2)
                {
                    ItemType matchType = kvp.Key;
                    
                    // Find a matching item in another slot
                    foreach (var sourceSlot in slots)
                    {
                        if (sourceSlot == targetSlot) continue;
                        
                        sourceSlot.SyncItemsFromCells();
                        var sourceItems = sourceSlot.GetComponentsInChildren<ItemView>(true);
                        
                        foreach (var sourceItem in sourceItems)
                        {
                            if (sourceItem != null && sourceItem.Type == matchType)
                            {
                                // FOUND A MATCH OPPORTUNITY!
                                // Decide action based on target slot state:
                                if (targetSlot.HasSpace)
                                {
                                    // ACTION 1: MOVE (slot has space)
                                    CompleteMatchByMove(targetSlot, sourceSlot, sourceItem, matchType);
                                }
                                else
                                {
                                    // ACTION 2: SWAP (slot is full - replace the odd item)
                                    // Find the "odd one out" item to swap away
                                    ItemView itemToReplace = null;
                                    foreach (var item in targetItems)
                                    {
                                        if (item != null && item.Type != matchType)
                                        {
                                            itemToReplace = item;
                                            break;
                                        }
                                    }
                                    
                                    // Only swap if there's actually an odd item (should always be true for full slots with 2 matching)
                                    if (itemToReplace != null)
                                    {
                                        CompleteMatchBySwap(targetSlot, sourceSlot, itemToReplace, sourceItem, matchType);
                                    }
                                }
                                
                                // Match completed - exit early
                                return;
                            }
                        }
                    }
                }
            }
        }
        
        // No match found (shouldn't happen due to HasAvailableMatch check)
        UpdateButtonState();
    }

    private void CompleteMatchByMove(SlotView targetSlot, SlotView sourceSlot, ItemView itemToMove, ItemType matchType)
    {
        // 1. Remove item from source slot
        sourceSlot.RemoveItem(itemToMove);
        
        // Detach from source hierarchy
        var rect = itemToMove.GetComponent<RectTransform>();
        if (rect != null && rect.parent != null)
        {
            rect.SetParent(null, true);
            rect.localScale = Vector3.one;
        }

        // 2. Add to target slot - THIS AUTOMATICALLY TRIGGERS TryResolveMatch() internally!
        if (targetSlot.TryAddItem(itemToMove))
        {
            // Update DragItem reference
            var dragItem = itemToMove.GetComponent<DragItem>();
            if (dragItem != null)
            {
                dragItem.SetCurrentSlot(targetSlot);
            }
            
            // 3. Arrange source slot items visually
            sourceSlot.ArrangeItems();
            
            // 4. Play feedback effects
            PlayFeedbackEffects(targetSlot.transform.position, matchType);
            
            Debug.Log($"[HammerPowerUp] Moved item to complete {matchType} match in {targetSlot.name}");
        }
        else
        {
            // Failed to add - return item to source slot
            sourceSlot.TryAddItem(itemToMove);
            sourceSlot.ArrangeItems();
            Debug.LogWarning("[HammerPowerUp] Failed to add item to target slot during MOVE");
        }

        // 5. Update power-up state
        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState();
    }

    private void CompleteMatchBySwap(SlotView targetSlot, SlotView sourceSlot, ItemView itemToReplace, ItemView itemToBring, ItemType matchType)
    {
        // 1. Remove both items from their slots
        targetSlot.RemoveItem(itemToReplace);
        sourceSlot.RemoveItem(itemToBring);
        
        // 2. Detach from hierarchies
        var rectReplace = itemToReplace.GetComponent<RectTransform>();
        var rectBring = itemToBring.GetComponent<RectTransform>();
        
        if (rectReplace != null && rectReplace.parent != null)
        {
            rectReplace.SetParent(null, true);
            rectReplace.localScale = Vector3.one;
        }
        
        if (rectBring != null && rectBring.parent != null)
        {
            rectBring.SetParent(null, true);
            rectBring.localScale = Vector3.one;
        }

        // 3. SWAP: Put matching item into target slot, odd item into source slot
        bool addedToTarget = targetSlot.TryAddItem(itemToBring);
        bool addedToSource = sourceSlot.TryAddItem(itemToReplace);
        
        if (addedToTarget)
        {
            // Update DragItem references
            var dragBring = itemToBring.GetComponent<DragItem>();
            if (dragBring != null)
            {
                dragBring.SetCurrentSlot(targetSlot);
            }
            
            // Target slot now has 3 matching items → match auto-resolves via TryAddItem()
        }
        
        if (addedToSource)
        {
            var dragReplace = itemToReplace.GetComponent<DragItem>();
            if (dragReplace != null)
            {
                dragReplace.SetCurrentSlot(sourceSlot);
            }
            
            sourceSlot.ArrangeItems();
        }

        // 4. Play feedback effects
        PlayFeedbackEffects(targetSlot.transform.position, matchType);
        
        Debug.Log($"[HammerPowerUp] Swapped items to complete {matchType} match in {targetSlot.name}");

        // 5. Update power-up state
        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState();
    }

    private void PlayFeedbackEffects(Vector3 position, ItemType matchType)
    {
        if (hammerEffect != null)
        {
            var effect = Instantiate(hammerEffect, position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }
        
        if (audioSource != null && matchSound != null)
        {
            audioSource.PlayOneShot(matchSound);
        }
    }

    // Optional: Reset power-up for new game
    public void ResetPowerUp()
    {
        remainingUses = maxUses;
        lastUsedTime = -999f;
        UpdateButtonState();
    }
}