using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SlotView : MonoBehaviour
{
    [SerializeField] private int capacity = 3;

    [SerializeField] private List<RectTransform> cells = new List<RectTransform>();
     private MatchCounter matchCounter;

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
    public bool TryAddItemToCell(ItemView item, RectTransform targetCell)
    {
        if (item == null || targetCell == null) return false;

        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);

        if (targetCell.parent != transform) return false;

        if (targetCell.childCount > 0) return false;

        if (!HasSpace) return false;
        if (items.Contains(item)) return true;

        items.Add(item);

        var itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(targetCell, false);
        itemRect.anchoredPosition = Vector2.zero;

        TryResolveMatch();
        return true;
    }

    public void SyncItemsFromCells()
    {
        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);

        items.Clear();
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell == null || cell.childCount == 0) continue;

            var item = cell.GetComponentInChildren<ItemView>(true);
            if (item != null && !items.Contains(item))
                items.Add(item);
        }
    }

    public bool TryAddItem(ItemView item)
    {
        if (item == null) return false;

        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);

        if (!HasSpace) return false;
        if (items.Contains(item)) return true;

        var emptyCell = GetFirstEmptyCell();
        if (emptyCell == null) return false;

        items.Add(item);

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
    }

   
    public void ArrangeItems()
    {
        CacheCellsIfNeeded();
        capacity = Mathf.Max(1, cells.Count);

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
