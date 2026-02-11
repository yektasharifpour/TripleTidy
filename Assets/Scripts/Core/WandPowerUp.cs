using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WandPowerUp : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Maximum number of times this power-up can be used per game")]
    [SerializeField] private int maxUses = 3;
    
    [Tooltip("Number of items to convert when wand is used")]
    [SerializeField] private int itemsToConvert = 3;
    
    [Tooltip("Cooldown in seconds between uses")]
    [SerializeField] private float cooldownSeconds = 1.5f;
    
    [Tooltip("Optional visual effect when wand converts items")]
    [SerializeField] private ParticleSystem wandEffect;
    
    [Tooltip("Optional sound to play when items are converted")]
    [SerializeField] private AudioClip convertSound;

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
        if (audioSource == null && convertSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        UpdateButtonState();
    }

    private void Start()
    {
        button.onClick.AddListener(ExecuteWand);
    }

    private void Update()
    {
        UpdateButtonState();
        // Re-enable button after cooldown expires
        // if (remainingUses > 0 && Time.time - lastUsedTime < cooldownSeconds)
        // {
        //     UpdateButtonState();
        //     wasOnCooldownLastFrame = true;
        // }
        // else if (wasOnCooldownLastFrame)
        // {
        //     UpdateButtonState();
        //     wasOnCooldownLastFrame = false;
        // }
    }

    private void UpdateButtonState()
    {
        bool onCooldown = Time.time - lastUsedTime < cooldownSeconds;
        bool hasItemsToConvert = HasItemsAvailable();
        button.interactable = remainingUses > 0 && !onCooldown && hasItemsToConvert;

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
            else if (!hasItemsToConvert)
                buttonText.text = "No Items";
            else
                buttonText.text = $"Wand ({remainingUses})";
        }
    }

    private bool HasItemsAvailable()
    {
        var items = FindObjectsOfType<ItemView>(true);
        Debug.Log($"[Wand] Found {items?.Length ?? 0} active ItemView objects");
        return items != null && items.Length > 0;
    }

    public void ExecuteWand()
    {
        // Safety checks
        if (remainingUses <= 0) return;
        if (Time.time - lastUsedTime < cooldownSeconds) return;
        if (!HasItemsAvailable()) return;

        var allItems = FindObjectsOfType<ItemView>(true);
        if (allItems == null || allItems.Length == 0) return;

        // Track which slots need match checking after conversion
        var slotsToCheck = new HashSet<SlotView>();

        // Convert random items
        int itemsConverted = 0;
        var shuffledItems = new List<ItemView>(allItems);
        ShuffleList(shuffledItems);

        foreach (var item in shuffledItems)
        {
            if (item == null || item.gameObject == null) continue;
            if (itemsConverted >= itemsToConvert) break;

            // Find which slot this item belongs to (via transform hierarchy)
            var slot = item.GetComponentInParent<SlotView>();
            if (slot != null)
                slotsToCheck.Add(slot);

            // Convert to a different random type
            ItemType currentType = item.Type;
            ItemType newType = GetRandomDifferentType(currentType);
            
            // Apply conversion
            item.SetType(newType);
            itemsConverted++;

            // Visual feedback per item
            if (wandEffect != null)
            {
                var effect = Instantiate(wandEffect, item.transform.position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration);
            }

            Debug.Log($"[WandPowerUp] Converted item from {currentType} → {newType}");
        }

        // Trigger match resolution on affected slots
        // Since changing type doesn't trigger TryResolveMatch() automatically,
        // we temporarily remove and re-add each item in these slots to trigger it
        foreach (var slot in slotsToCheck)
        {
            if (slot == null) continue;
            
            slot.SyncItemsFromCells();
            var itemsInSlot = slot.GetComponentsInChildren<ItemView>(true);
            
            // Temporarily remove and re-add items to trigger TryResolveMatch()
            foreach (var item in itemsInSlot)
            {
                if (item == null) continue;
                
                slot.RemoveItem(item);
                slot.TryAddItem(item);
                
                // Update DragItem's slot reference
                var dragItem = item.GetComponent<DragItem>();
                if (dragItem != null)
                {
                    dragItem.SetCurrentSlot(slot);
                }
            }
            
            slot.ArrangeItems();
        }

        // Play sound effect
        if (audioSource != null && convertSound != null)
        {
            audioSource.PlayOneShot(convertSound);
        }

        // Update power-up state
        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState();

        Debug.Log($"[WandPowerUp] Converted {itemsConverted} items. Checked {slotsToCheck.Count} slots for matches.");
    }

    private ItemType GetRandomDifferentType(ItemType currentType)
    {
        var allTypes = System.Enum.GetValues(typeof(ItemType));
        var possibleTypes = new List<ItemType>();
        
        foreach (ItemType type in allTypes)
        {
            if (type != currentType)
                possibleTypes.Add(type);
        }
        
        if (possibleTypes.Count == 0)
            return currentType;
        
        return possibleTypes[Random.Range(0, possibleTypes.Count)];
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