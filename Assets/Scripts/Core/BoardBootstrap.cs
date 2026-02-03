using UnityEngine;

public class BoardBootstrap : MonoBehaviour
{
    private void Start()
    {
        var slots = GetComponentsInChildren<SlotView>(true);

        foreach (var slot in slots)
        {
            slot.CacheCellsIfNeeded();
            slot.SyncItemsFromCells();

            // آیتم‌هایی که داخل Cellها هستند هم پیدا می‌شوند
            var items = slot.GetComponentsInChildren<ItemView>(true);

            foreach (var item in items)
            {
                var drag = item.GetComponent<DragItem>();
                if (drag != null)
                    drag.SetCurrentSlot(slot);
            }

            slot.ArrangeItems();
        }
    }
}
