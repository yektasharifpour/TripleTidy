using UnityEngine;

public class BoardBootstrap : MonoBehaviour
{
    private void Start()
    {
        var slots = GetComponentsInChildren<SlotView>(true);
        foreach (var slot in slots)
        {
            var items = slot.GetComponentsInChildren<ItemView>(true);
            foreach (var item in items)
            {
                slot.TryAddItem(item);

                var drag = item.GetComponent<DragItem>();
                if (drag != null)
                    drag.SetCurrentSlot(slot);
            }
        }
    }
}
