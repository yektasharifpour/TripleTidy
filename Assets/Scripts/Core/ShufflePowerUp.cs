using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ShufflePowerUp : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Maximum number of times this power-up can be used per game")]
    [SerializeField] private int maxUses = 3;
    
    [Tooltip("Cooldown in seconds between uses (0 = no cooldown)")]
    [SerializeField] private float cooldownSeconds = 2f;
    
    [Tooltip("Optional visual feedback when power-up is used")]
    [SerializeField] private ParticleSystem shuffleEffect;
    
    [Tooltip("Optional sound to play when shuffling")]
    [SerializeField] private AudioClip shuffleSound;

    private Button button;
    private int remainingUses;
    private float lastUsedTime = -999f;
    private AudioSource audioSource;
    private Text buttonText;
    private bool wasOnCooldownLastFrame = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>(true);
        
        remainingUses = maxUses;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && shuffleSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        UpdateButtonState();
    }

    private void Start()
    {
        button.onClick.AddListener(ExecuteShuffle);
    }

    private void Update()
    {
        // Only check cooldown state when we're actually on cooldown (avoids unnecessary checks)
        if (remainingUses > 0 && Time.time - lastUsedTime < cooldownSeconds)
        {
            // Still on cooldown - update button state to show countdown
            UpdateButtonState();
            wasOnCooldownLastFrame = true;
        }
        else if (wasOnCooldownLastFrame)
        {
            // Cooldown just ended - update state once
            UpdateButtonState();
            wasOnCooldownLastFrame = false;
        }
    }

    private void UpdateButtonState()
    {
        bool onCooldown = Time.time - lastUsedTime < cooldownSeconds;
        button.interactable = remainingUses > 0 && !onCooldown;

        // Update button text if a Text component exists
        if (buttonText != null)
        {
            if (remainingUses <= 0)
                buttonText.text = "No Uses Left";
            else if (onCooldown)
            {
                float remainingCooldown = Mathf.CeilToInt(cooldownSeconds - (Time.time - lastUsedTime));
                buttonText.text = $"Wait {remainingCooldown}s";
            }
            else
                buttonText.text = $"Shuffle ({remainingUses})";
        }
    }

    public void ExecuteShuffle()
    {
        // Safety checks
        if (remainingUses <= 0) return;
        if (Time.time - lastUsedTime < cooldownSeconds) return;

        // 1. Find all slots in the scene
        var slots = FindObjectsOfType<SlotView>(true);
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning("[ShufflePowerUp] No SlotView components found in scene!");
            return;
        }

        // 2. Collect all items currently in slots
        var allItems = new List<ItemView>();
        foreach (var slot in slots)
        {
            slot.SyncItemsFromCells(); // Ensure internal state is up-to-date
            
            // Get items via transform hierarchy
            var itemsInSlot = slot.GetComponentsInChildren<ItemView>(true);
            foreach (var item in itemsInSlot)
            {
                if (item == null || item.gameObject == null) continue;
                
                allItems.Add(item);
                
                // Remove from current slot
                slot.RemoveItem(item);
                
                // Detach from slot hierarchy
                var rect = item.GetComponent<RectTransform>();
                if (rect != null && rect.parent != null)
                {
                    rect.SetParent(null, true);
                    rect.localScale = Vector3.one; // Reset scale
                }
            }
        }

        // Nothing to shuffle
        if (allItems.Count == 0)
        {
            Debug.LogWarning("[ShufflePowerUp] No items to shuffle!");
            return;
        }

        // 3. Shuffle items randomly
        ShuffleList(allItems);

        // 4. Redistribute items to slots (respecting capacity via TryAddItem)
        int itemIndex = 0;
        while (itemIndex < allItems.Count)
        {
            bool placed = false;
            
            // Try slots in random order to avoid bias
            var shuffledSlots = new List<SlotView>(slots);
            ShuffleList(shuffledSlots);
            
            foreach (var slot in shuffledSlots)
            {
                if (slot.HasSpace && slot.TryAddItem(allItems[itemIndex]))
                {
                    // Update DragItem's slot reference
                    var dragItem = allItems[itemIndex].GetComponent<DragItem>();
                    if (dragItem != null)
                    {
                        dragItem.SetCurrentSlot(slot);
                    }
                    
                    itemIndex++;
                    placed = true;
                    break;
                }
            }
            
            // Safety break - shouldn't happen with valid board setup
            if (!placed)
            {
                Debug.LogWarning($"[ShufflePowerUp] Could not place item {itemIndex} - insufficient slot capacity!");
                break;
            }
        }

        // 5. Arrange items visually in all slots
        foreach (var slot in slots)
        {
            slot.ArrangeItems();
        }

        // 6. Play feedback effects
        if (shuffleEffect != null)
        {
            var effect = Instantiate(shuffleEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }

        if (audioSource != null && shuffleSound != null)
        {
            audioSource.PlayOneShot(shuffleSound);
        }

        // 7. Update power-up state
        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState();

        Debug.Log($"[ShufflePowerUp] Successfully shuffled {itemIndex} items across {slots.Length} slots.");
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
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