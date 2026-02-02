using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;



[RequireComponent(typeof(RectTransform))]
public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas rootCanvas;
    private SlotView currentSlot;
    private SlotView previousSlot;
    private ItemView itemView;



    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        itemView = GetComponent<ItemView>();


        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (currentSlot == null)
            currentSlot = GetComponentInParent<SlotView>();
    }

    public void SetCurrentSlot(SlotView slot)
    {
        currentSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalAnchoredPos = rect.anchoredPosition;

        // ✅ همینجا آیتم را از اسلات فعلی حذف کن تا لیست‌ها دقیق بمانند
        previousSlot = currentSlot;
        if (currentSlot != null)
            currentSlot.RemoveItem(itemView);

        // می‌بریم زیر Canvas برای اینکه روی همه چیز بیاد
        transform.SetParent(rootCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;
    }


    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // ✅ اول مقصد رو پیدا کن در حالی که blocksRaycasts هنوز false است
        SlotView targetSlot = GetSlotUnderPointer(eventData);

        // ✅ بعدش Raycast آیتم رو روشن کن
        canvasGroup.blocksRaycasts = true;

        if (targetSlot != null && targetSlot.HasSpace)
        {
            targetSlot.TryAddItem(itemView);
            currentSlot = targetSlot;

            transform.SetParent(targetSlot.transform, true);
            targetSlot.ArrangeItems();
        }
        else
        {
            // نامعتبر → برگرد به اسلات قبلی
            if (previousSlot != null)
            {
                previousSlot.TryAddItem(itemView);
                currentSlot = previousSlot;

                transform.SetParent(previousSlot.transform, true);
                previousSlot.ArrangeItems();
            }
            else
            {
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
            // ✅ خود آیتمی که داریم می‌کشیم رو نادیده بگیر
            if (r.gameObject == gameObject || r.gameObject.transform.IsChildOf(transform))
                continue;

            var slot = r.gameObject.GetComponentInParent<SlotView>();
            if (slot != null)
                return slot;
        }

        return null;
    }





}
