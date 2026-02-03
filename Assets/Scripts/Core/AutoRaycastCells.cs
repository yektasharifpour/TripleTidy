using UnityEngine;
using UnityEngine.UI;

public class AutoRaycastCells : MonoBehaviour
{
    [SerializeField] private bool includeInactive = true;

    private void Awake()
    {
        var slots = FindObjectsOfType<SlotView>(includeInactive);

        foreach (var slot in slots)
        {
            for (int i = 0; i < slot.transform.childCount; i++)
            {
                var cell = slot.transform.GetChild(i);

                var img = cell.GetComponent<Image>();
                if (img == null)
                    img = cell.gameObject.AddComponent<Image>();

                var c = img.color;
                c.a = 0f;
                img.color = c;

                img.raycastTarget = true;
            }
        }
    }
}
