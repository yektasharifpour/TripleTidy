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
    private bool wasOnCooldownLastFrame = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        
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
        if (remainingUses > 0 && Time.time - lastUsedTime < cooldownSeconds)
        {
            UpdateButtonState();
            wasOnCooldownLastFrame = true;
        }
        else if (wasOnCooldownLastFrame)
        {
            UpdateButtonState();
            wasOnCooldownLastFrame = false;
        }
    }

    private void UpdateButtonState()
    {
        bool onCooldown = Time.time - lastUsedTime < cooldownSeconds;
        button.interactable = remainingUses > 0 && !onCooldown;
    }

    public void ExecuteShuffle()
    {
        if (remainingUses <= 0) return;
        if (Time.time - lastUsedTime < cooldownSeconds) return;

        var slots = FindObjectsOfType<SlotView>(true);
        if (slots == null || slots.Length == 0) return;

        var allItems = new List<ItemView>();
        foreach (var slot in slots)
        {
            slot.SyncItemsFromCells();
            
            var itemsInSlot = slot.GetComponentsInChildren<ItemView>(true);
            foreach (var item in itemsInSlot)
            {
                if (item == null || item.gameObject == null) continue;
                
                allItems.Add(item);
                
                slot.RemoveItem(item);
                
                var rect = item.GetComponent<RectTransform>();
                if (rect != null && rect.parent != null)
                {
                    rect.SetParent(null, true);
                    rect.localScale = Vector3.one;
                }
            }
        }

        if (allItems.Count == 0) return;

        ShuffleList(allItems);

        int itemIndex = 0;
        while (itemIndex < allItems.Count)
        {
            bool placed = false;
            
            var shuffledSlots = new List<SlotView>(slots);
            ShuffleList(shuffledSlots);
            
            foreach (var slot in shuffledSlots)
            {
                if (slot.HasSpace && slot.TryAddItem(allItems[itemIndex]))
                {
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
            
            if (!placed) break;
        }

        foreach (var slot in slots)
        {
            slot.ArrangeItems();
        }

        if (shuffleEffect != null)
        {
            var effect = Instantiate(shuffleEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }

        if (audioSource != null && shuffleSound != null)
        {
            audioSource.PlayOneShot(shuffleSound);
        }

        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void ResetPowerUp()
    {
        remainingUses = maxUses;
        lastUsedTime = -999f;
        UpdateButtonState();
    }
}