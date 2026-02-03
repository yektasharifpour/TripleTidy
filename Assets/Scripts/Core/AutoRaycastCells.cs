using UnityEngine;
using UnityEngine.UI;

public class AutoRaycastCells : MonoBehaviour
{
    [SerializeField] private bool includeInactive = true;

    private void Awake()
    {
        // همه Slotها رو پیدا کن
        var slots = FindObjectsOfType<SlotView>(includeInactive);

        foreach (var slot in slots)
        {
            // بچه‌های مستقیم Slot همون Cellها هستن
            for (int i = 0; i < slot.transform.childCount; i++)
            {
                var cell = slot.transform.GetChild(i);

                // اگر Cell خودش SlotView یا چیز دیگری بود، ردش کن (اینجا معمولاً لازم نیست)
                // مهم: باید Graphic داشته باشه تا Raycast بگیره
                var img = cell.GetComponent<Image>();
                if (img == null)
                    img = cell.gameObject.AddComponent<Image>();

                // شفاف ولی Raycastable
                var c = img.color;
                c.a = 0f;
                img.color = c;

                img.raycastTarget = true;
            }
        }
    }
}
