using UnityEngine;

public class BoardBootstrap : MonoBehaviour
{
    private void Start()
    {
        var slots = GetComponentsInChildren<SlotView>(true);

        foreach (var slot in slots)
        {
            slot.CacheCellsIfNeeded();

            // آیتم‌هایی که داخل Cellها هستند هم پیدا می‌شوند
            var items = slot.GetComponentsInChildren<ItemView>(true);

            foreach (var item in items)
            {
                // چون در Start صحنه ممکن است items از قبل زیر slot باشند، دوباره اضافه می‌کنیم
                slot.TryAddItem(item);

                var drag = item.GetComponent<DragItem>();
                if (drag != null)
                    drag.SetCurrentSlot(slot);
            }

            slot.ArrangeItems();
        }
    }
}
