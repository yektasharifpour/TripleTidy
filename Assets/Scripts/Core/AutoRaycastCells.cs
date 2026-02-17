using UnityEngine;
using UnityEngine.UI;

public class AutoRaycastCells : MonoBehaviour
{
    [SerializeField] private bool includeInactive = t    [SerializeField] private bool refreshEveryFrame = false;

    private bool refreshQueued = false;

    private void Awake()
    {
        QueueRefresh();
    }

    private void OnEnable()
    {
        QueueRefresh();
    }

    private void OnTransformChildrenChanged()
    {
        QueueRefresh();
    }

    private void LateUpdate()
    {
        if (refreshEveryFrame)
        {
            RefreshRaycastTargets();
            return;
        }

        if (!refreshQueued) return;
        refreshQueued = false;
        RefreshRaycastTargets();
    }

    public void RefreshRaycastTargets()
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

    private void QueueRefresh()
    {
        refreshQueued = true;

            }
        }
    }
}
