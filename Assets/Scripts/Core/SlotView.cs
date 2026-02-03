using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SlotView : MonoBehaviour
{
    [SerializeField] private int capacity = 3;

    // Cell های داخل Slot (بچه‌های مستقیم Slot)
    [SerializeField] private List<RectTransform> cells = new List<RectTransform>();
     private MatchCounter matchCounter;

    // آیتم‌هایی که این Slot مالک‌شونه (برای منطق)
    private readonly List<ItemView> items = new List<ItemView>();

    public bool HasSpace => items.Count < capacity;
    private void Start()
    {
        if (matchCounter == null)
        {
            matchCounter = FindObjectOfType<MatchCounter>();
        }
    }
    private void Awake()
    {
        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);
    }

    /// <summary>
    /// اگر cells از Inspector ست نشده بود، از بچه‌های مستقیم Slot جمع‌آوری می‌کند.
    /// </summary>
    public void CacheCellsIfNeeded()
    {
        if (cells != null && cells.Count > 0) return;

        cells = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt != null)
                cells.Add(rt);
        }
    }

    public bool TryAddItem(ItemView item)
    {
        if (item == null) return false;

        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);

        if (!HasSpace) return false;
        if (items.Contains(item)) return true;

        // اولین Cell خالی را پیدا کن
        var emptyCell = GetFirstEmptyCell();
        if (emptyCell == null) return false;

        items.Add(item);

        // آیتم را بفرست داخل Cell
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(emptyCell, false);
        itemRect.anchoredPosition = Vector2.zero;

        TryResolveMatch();

        return true;
    }

    public void RemoveItem(ItemView item)
    {
        if (item == null) return;

        items.Remove(item);
        // parent آیتم را اینجا تغییر نمی‌دهیم؛ DragItem هنگام Drag به Canvas می‌برد
    }

    /// <summary>
    /// برای زمانی که لازم داری ترتیب آیتم‌ها دوباره داخل Cellها مرتب شود.
    /// </summary>
    public void ArrangeItems()
    {
        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);

        // همه آیتم‌ها را دوباره در Cellها از اول بچین
        int index = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (index >= cells.Count) break;

            var item = items[i];
            var itemRect = item.GetComponent<RectTransform>();

            itemRect.SetParent(cells[index], false);
            itemRect.anchoredPosition = Vector2.zero;

            index++;
        }
    }

    private void TryResolveMatch()
    {
        if (items.Count != capacity) return;

        var firstType = items[0].Type;
        for (int i = 1; i < items.Count; i++)
        {
            if (items[i].Type != firstType)
                return;
        }

        var matchedCount = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                Destroy(items[i].gameObject);
        }
        items.Clear();

        matchCounter.AddMatches(matchedCount / 3);

        if (FindObjectsOfType<ItemView>(true).Length == 0)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private RectTransform GetFirstEmptyCell()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == null) continue;
            if (cells[i].childCount == 0)
                return cells[i];
        }
        return null;
    }
}
