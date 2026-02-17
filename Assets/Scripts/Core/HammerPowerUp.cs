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

    [Tooltip("How often to scan the board for available matches (seconds)")]
    [SerializeField] private float availabilityScanInterval = 0.25f;
    
    [Tooltip("Optional visual effect when hammer completes a match")]
    [SerializeField] private ParticleSystem hammerEffect;
    
    [Tooltip("Optional sound to play when match is completed")]
    [SerializeField] private AudioClip matchSound;

    private Button button;
    private int remainingUses;
    private float lastUsedTime = -999f;
    private AudioSource audioSource;
    private float nextAvailabilityScanTime = 0f;
    private bool cachedHasAvailableMatch = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        
        remainingUses = maxUses;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && matchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        UpdateButtonState(true);
    }

    private void Start()
    {
        button.onClick.AddListener(ExecuteHammer);
    }

    private void Update()
    {
        UpdateButtonState();
    }

    private void UpdateButtonState(bool forceRescan = false)
    {
        if (remainingUses <= 0)
        {
            button.interactable = false;
            return;
        }

        bool onCooldown = Time.time - lastUsedTime < cooldownSeconds;
        if (onCooldown)
        {
            button.interactable = false;
            return;
        }

        if (forceRescan || Time.time >= nextAvailabilityScanTime)
        {
            cachedHasAvailableMatch = HasAvailableMatch();
            nextAvailabilityScanTime = Time.time + availabilityScanInterval;
        }

        button.interactable = cachedHasAvailableMatch;
    }

    private bool HasAvailableMatch()
    {
        var slots = FindObjectsOfType<SlotView>(true);
        if (slots == null || slots.Length == 0) return false;

        foreach (var slot in slots)
        {
            slot.SyncItemsFromCells();
            var items = slot.GetComponentsInChildren<ItemView>(true);
            
            if (items.Length < 2) continue;
            
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
            
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value >= 2)
                {
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
        if (remainingUses <= 0) return;
        if (Time.time - lastUsedTime < cooldownSeconds) return;
        if (!HasAvailableMatch()) return;

        var slots = FindObjectsOfType<SlotView>(true);
        if (slots == null || slots.Length == 0) return;

        foreach (var targetSlot in slots)
        {
            targetSlot.SyncItemsFromCells();
            var targetItems = targetSlot.GetComponentsInChildren<ItemView>(true);
            
            if (targetItems.Length < 2) continue;
            
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
            
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value >= 2)
                {
                    ItemType matchType = kvp.Key;
                    
                    foreach (var sourceSlot in slots)
                    {
                        if (sourceSlot == targetSlot) continue;
                        
                        sourceSlot.SyncItemsFromCells();
                        var sourceItems = sourceSlot.GetComponentsInChildren<ItemView>(true);
                        
                        foreach (var sourceItem in sourceItems)
                        {
                            if (sourceItem != null && sourceItem.Type == matchType)
                            {
                                if (targetSlot.HasSpace)
                                {
                                    CompleteMatchByMove(targetSlot, sourceSlot, sourceItem, matchType);
                                }
                                else
                                {
                                    ItemView itemToReplace = null;
                                    foreach (var item in targetItems)
                                    {
                                        if (item != null && item.Type != matchType)
                                        {
                                            itemToReplace = item;
                                            break;
                                        }
                                    }
                                    
                                    if (itemToReplace != null)
                                    {
                                        CompleteMatchBySwap(targetSlot, sourceSlot, itemToReplace, sourceItem, matchType);
                                    }
                                }
                                
                                return;
                            }
                        }
                    }
                }
            }
        }
        
        UpdateButtonState();
    }

    private void CompleteMatchByMove(SlotView targetSlot, SlotView sourceSlot, ItemView itemToMove, ItemType matchType)
    {
        sourceSlot.RemoveItem(itemToMove);
        
        var rect = itemToMove.GetComponent<RectTransform>();
        if (rect != null && rect.parent != null)
        {
            rect.SetParent(null, true);
            rect.localScale = Vector3.one;
        }

        if (targetSlot.TryAddItem(itemToMove))
        {
            var dragItem = itemToMove.GetComponent<DragItem>();
            if (dragItem != null)
            {
                dragItem.SetCurrentSlot(targetSlot);
            }
            
            sourceSlot.ArrangeItems();
            
            PlayFeedbackEffects(targetSlot.transform.position, matchType);
        }
        else
        {
            sourceSlot.TryAddItem(itemToMove);
            sourceSlot.ArrangeItems();
        }

        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState(true);
    }

    private void CompleteMatchBySwap(SlotView targetSlot, SlotView sourceSlot, ItemView itemToReplace, ItemView itemToBring, ItemType matchType)
    {
        targetSlot.RemoveItem(itemToReplace);
        sourceSlot.RemoveItem(itemToBring);
        
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

        bool addedToTarget = targetSlot.TryAddItem(itemToBring);
        bool addedToSource = sourceSlot.TryAddItem(itemToReplace);
        
        if (addedToTarget)
        {
            var dragBring = itemToBring.GetComponent<DragItem>();
            if (dragBring != null)
            {
                dragBring.SetCurrentSlot(targetSlot);
            }
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

        PlayFeedbackEffects(targetSlot.transform.position, matchType);

        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState(true);
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

    public void ResetPowerUp()
    {
        remainingUses = maxUses;
        lastUsedTime = -999f;
        UpdateButtonState(true);
    }
}
