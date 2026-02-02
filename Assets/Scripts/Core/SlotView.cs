using System.Collections.Generic;
using UnityEngine;

public class SlotView : MonoBehaviour
{
    [SerializeField] private int capacity = 3;
    [SerializeField] private float offset = 35f;

    private readonly List<ItemView> items = new List<ItemView>();

    public bool HasSpace => items.Count < capacity;

    public bool TryAddItem(ItemView item)
    {
        if (item == null) return false;
        if (!HasSpace) return false;

        if (!items.Contains(item))
            items.Add(item);

        ArrangeItems();
        return true;
    }

    public void RemoveItem(ItemView item)
    {
        if (item == null) return;

        if (items.Remove(item))
            ArrangeItems();
    }

    public void ArrangeItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var rect = items[i].GetComponent<RectTransform>();
            if (rect == null) continue;

            rect.SetParent(transform, true);
            rect.anchoredPosition = GetPos(i);
        }
    }

    private Vector2 GetPos(int i)
    {
        // 3 جایگاه برای ظرفیت 3
        if (i == 0) return new Vector2(-offset, offset);
        if (i == 1) return new Vector2(offset, offset);
        return new Vector2(0f, -offset);
    }
}
