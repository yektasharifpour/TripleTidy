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
    private Vector2 dragOffset;
    private RectTransform rootCanvasRect;

    private ItemView itemView;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        itemView = GetComponent<ItemView>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        rootCanvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;

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

        previousSlot = currentSlot;

        if (previousSlot != null)
            previousSlot.RemoveItem(itemView);

        if (rootCanvas != null)
            transform.SetParent(rootCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;

        if (rootCanvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvasRect, eventData.position, eventData.pressEventCamera, out var localPointerPos))
        {
            dragOffset = rect.anchoredPosition - localPointerPos;
        }
        else
        {
            dragOffset = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvasRect, eventData.position, eventData.pressEventCamera, out var localPointerPos))
        {
            rect.anchoredPosition = localPointerPos + dragOffset;
        }
        else if (rootCanvas != null)
        {
            rect.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SlotView targetSlot;
        RectTransform targetCell = GetCellUnderPointer(eventData, out targetSlot);

        canvasGroup.blocksRaycasts = true;


        bool placed = false;

        if (targetSlot != null && targetSlot.HasSpace)
        {
            if (targetCell != null)
                placed = targetSlot.TryAddItemToCell(itemView, targetCell);

            if (!placed)
                placed = targetSlot.TryAddItem(itemView);

            if (placed)
                currentSlot = targetSlot;

        }

        if (!placed)
        {
            if (previousSlot != null)
            {
                previousSlot.TryAddItem(itemView);
                currentSlot = previousSlot;
            }
            else
            {
                transform.SetParent(originalParent, true);
                rect.anchoredPosition = originalAnchoredPos;
            }
        }

        previousSlot = null;
    }

    private RectTransform GetCellUnderPointer(PointerEventData eventData, out SlotView slot)
    {
        slot = null;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == gameObject || r.gameObject.transform.IsChildOf(transform))
                continue;

            var s = r.gameObject.GetComponentInParent<SlotView>();
            if (s == null) continue;

            var cell = r.gameObject.GetComponent<RectTransform>();
            if (cell != null && cell.parent == s.transform)
            {
                slot = s;
                return cell;
            }

            slot = s;
        }

        return null;
    }


}
