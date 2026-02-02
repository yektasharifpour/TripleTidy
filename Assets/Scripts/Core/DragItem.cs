using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas rootCanvas;

    private SlotView currentSlot;
    private SlotView previousSlot;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;

    private ItemView itemView;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        itemView = GetComponent<ItemView>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        // چون آیتم داخل Cell است، Slot را از والدهای بالا پیدا می‌کند
        currentSlot = GetComponentInParent<SlotView>();
    }

    public void SetCurrentSlot(SlotView slot)
    {
        currentSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;          // معمولاً Cell
        originalAnchoredPos = rect.anchoredPosition; // معمولاً صفر

        previousSlot = currentSlot;

        // ✅ همینجا از Slot قبلی خارجش کن (برای همگام بودن لیست‌ها)
        if (previousSlot != null)
            previousSlot.RemoveItem(itemView);

        // آیتم را ببر زیر Canvas
        transform.SetParent(rootCanvas.transform, true);

        // هنگام Raycast برای Drop، خود آیتم مزاحم نشود
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // ✅ اول هدف را پیدا کن در حالی که blocksRaycasts هنوز false است
        var targetSlot = GetSlotUnderPointer(eventData);

        // بعد Raycast را روشن کن
        canvasGroup.blocksRaycasts = true;

        // اگر مقصد معتبر و ظرفیت داشت
        if (targetSlot != null && targetSlot.HasSpace)
        {
            targetSlot.TryAddItem(itemView);
            currentSlot = targetSlot;
            targetSlot.ArrangeItems();
        }
        else
        {
            // نامعتبر -> برگرد به Slot قبلی
            if (previousSlot != null)
            {
                previousSlot.TryAddItem(itemView);
                currentSlot = previousSlot;
                previousSlot.ArrangeItems();
            }
            else
            {
                // حالت نادر: اگر Slot قبلی نداشت
                transform.SetParent(originalParent, true);
                rect.anchoredPosition = originalAnchoredPos;
            }
        }

        previousSlot = null;
    }

    private SlotView GetSlotUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            // خود آیتم و بچه‌هاش را ignore کن
            if (r.gameObject == gameObject || r.gameObject.transform.IsChildOf(transform))
                continue;

            var slot = r.gameObject.GetComponentInParent<SlotView>();
            if (slot != null)
                return slot;
        }

        return null;
    }
}
