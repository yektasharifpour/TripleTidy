using System.Collections.Generic;
using UnityEngine;

public enum EmptyCellsOption
{
    _0 = 0,
    _3 = 3,
    _6 = 6,
    _9 = 9,
    _12 = 12,
    _15 = 15,
    _18 = 18
}

public class BoardBootstrap : MonoBehaviour
{
    [Header("Random Fill")]
    [SerializeField] private List<GameObject> itemPrefabs = new List<GameObject>();

    [Tooltip("How many cells should stay empty across the whole board (must be a multiple of 3).")]
    [SerializeField] private EmptyCellsOption emptyCells = EmptyCellsOption._3;

    [Header("Solver")]
    [Tooltip("Max attempts to find a valid layout with NO 3 identical prefabs inside the same Slot.")]
    [SerializeField] private int maxAttempts = 200;

    private void Start()
    {
        var slots = GetComponentsInChildren<SlotView>(true);

        var cellsFlat = new List<(int slotIndex, Transform cell)>(18);

        for (int s = 0; s < slots.Length; s++)
        {
            var slot = slots[s];
            slot.CacheCellsIfNeeded();

            for (int i = 0; i < slot.transform.childCount; i++)
            {
                if (cellsFlat.Count >= 18) break;
                cellsFlat.Add((s, slot.transform.GetChild(i)));
            }

            if (cellsFlat.Count >= 18) break;
        }

        int totalCells = cellsFlat.Count;
        int targetEmpty = Mathf.Clamp((int)emptyCells, 0, totalCells);
        int fillCount = totalCells - targetEmpty;

        if (fillCount < 0) fillCount = 0;

        if (fillCount % 3 != 0)
        {
            Debug.LogError(
                $"[BoardBootstrap] Invalid fillCount={fillCount}. " +
                $"To keep each prefab count as a multiple of 3, (18 - emptyCells) must be a multiple of 3."
            );
            return;
        }

        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogError("[BoardBootstrap] itemPrefabs list is empty. Please assign at least one prefab.");
            return;
        }
        int triplets = fillCount / 3;
        if (triplets < itemPrefabs.Count)
        {
            Debug.LogError(
                $"[BoardBootstrap] Not enough filled cells to guarantee at least one match per prefab. " +
                $"You have {itemPrefabs.Count} prefabs, so you need at least {itemPrefabs.Count * 3} filled cells, " +
                $"but fillCount is {fillCount}. Reduce prefab count or reduce emptyCells."
            );
            return;
        }


        if (!TryBuildSolution(cellsFlat, totalCells, targetEmpty, fillCount, out var willFill, out var assignedPrefabByCellIndex))
        {
            Debug.LogError(
                "[BoardBootstrap] Failed to build a valid layout with current settings. " +
                "This can happen if prefab diversity is too low for the 'no triple in a slot' constraint."
            );
            return;
        }

        for (int idx = 0; idx < totalCells; idx++)
        {
            var cell = cellsFlat[idx].cell;

            if (cell.childCount > 0) continue;

            if (!willFill[idx]) continue;

            var prefab = assignedPrefabByCellIndex[idx];
            if (prefab == null) continue;

            var go = Instantiate(prefab, cell, false);

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
        }

        foreach (var slot in slots)
        {
            slot.CacheCellsIfNeeded();
            slot.SyncItemsFromCells();

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

    public List<GameObject> GetItemPrefabs()
    {
        return itemPrefabs;
    }

    private bool TryBuildSolution(
        List<(int slotIndex, Transform cell)> cellsFlat,
        int totalCells,
        int targetEmpty,
        int fillCount,
        out bool[] willFill,
        out GameObject[] assignedPrefabByCellIndex)
    {
        willFill = new bool[totalCells];
        assignedPrefabByCellIndex = new GameObject[totalCells];

        var eligibleIndices = new List<int>(totalCells);
        for (int i = 0; i < totalCells; i++)
        {
            if (cellsFlat[i].cell.childCount == 0)
                eligibleIndices.Add(i);
        }

        int maxFillPossible = eligibleIndices.Count;
        int desiredFill = Mathf.Min(fillCount, maxFillPossible);
        desiredFill -= (desiredFill % 3); 

        if (desiredFill == 0)
        {
            return true;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            ClearArrays(willFill, assignedPrefabByCellIndex);

            var eligibleShuffled = new List<int>(eligibleIndices);
            Shuffle(eligibleShuffled);

            var fillIndices = new List<int>(desiredFill);
            for (int i = 0; i < desiredFill; i++)
                fillIndices.Add(eligibleShuffled[i]);

            foreach (var fi in fillIndices)
                willFill[fi] = true;

            var fillPerSlot = new Dictionary<int, int>();
            for (int i = 0; i < totalCells; i++)
            {
                if (!willFill[i]) continue;
                int s = cellsFlat[i].slotIndex;
                fillPerSlot[s] = fillPerSlot.TryGetValue(s, out var c) ? c + 1 : 1;
            }

            var bag = BuildTripletBag(desiredFill, fillPerSlot);
            if (bag == null) continue;

            if (TryAssignBagToCells_NoTripleInFullSlots(cellsFlat, willFill, fillPerSlot, bag, assignedPrefabByCellIndex))
                return true;
        }

        return false;

        List<GameObject> BuildTripletBag(int count, Dictionary<int, int> fillPerSlot)
        {
            if (count % 3 != 0) return null;

            int triplets = count / 3;

            if (triplets < itemPrefabs.Count)
                return null;

            var bag = new List<GameObject>(count);

            for (int p = 0; p < itemPrefabs.Count; p++)
            {
                var prefab = itemPrefabs[p];
                if (prefab == null) continue;

                bag.Add(prefab);
                bag.Add(prefab);
                bag.Add(prefab);
            }

            int remainingTriplets = triplets - itemPrefabs.Count;
            for (int t = 0; t < remainingTriplets; t++)
            {
                var prefab = itemPrefabs[Random.Range(0, itemPrefabs.Count)];
                if (prefab == null) { t--; continue; }

                bag.Add(prefab);
                bag.Add(prefab);
                bag.Add(prefab);
            }

            Shuffle(bag);
            return bag;
        }

    }

    private bool TryAssignBagToCells_NoTripleInFullSlots(
        List<(int slotIndex, Transform cell)> cellsFlat,
        bool[] willFill,
        Dictionary<int, int> fillPerSlot,
        List<GameObject> bag,
        GameObject[] assignedPrefabByCellIndex)
    {
        var fillIndices = new List<int>(bag.Count);
        for (int i = 0; i < willFill.Length; i++)
            if (willFill[i]) fillIndices.Add(i);

        if (fillIndices.Count < bag.Count)
            return false;

        var slotCounts = new Dictionary<int, Dictionary<GameObject, int>>();
        var slotFilledSoFar = new Dictionary<int, int>();

        int bagIndex = 0;

        for (int k = 0; k < fillIndices.Count && bagIndex < bag.Count; k++)
        {
            int cellIdx = fillIndices[k];
            int slot = cellsFlat[cellIdx].slotIndex;

            if (!slotCounts.TryGetValue(slot, out var dict))
            {
                dict = new Dictionary<GameObject, int>();
                slotCounts[slot] = dict;
                slotFilledSoFar[slot] = 0;
            }

            int needInSlot = fillPerSlot.TryGetValue(slot, out var v) ? v : 0;
            int filledInSlot = slotFilledSoFar[slot];

            bool isFullSlot = (needInSlot == 3);
            GameObject chosen;

            if (!isFullSlot)
            {
                chosen = bag[bagIndex++];
            }
            else
            {
                if (filledInSlot == 2)
                {
                    GameObject forbidden = null;
                    foreach (var pair in dict)
                    {
                        if (pair.Value >= 2)
                        {
                            forbidden = pair.Key;
                            break;
                        }
                    }

                    if (forbidden != null)
                    {
                        int foundAt = -1;
                        for (int j = bagIndex; j < bag.Count; j++)
                        {
                            if (bag[j] != forbidden)
                            {
                                foundAt = j;
                                break;
                            }
                        }

                        if (foundAt == -1)
                            return false; 

                        (bag[bagIndex], bag[foundAt]) = (bag[foundAt], bag[bagIndex]);
                    }
                }

                chosen = bag[bagIndex++];
            }

            assignedPrefabByCellIndex[cellIdx] = chosen;

            if (!dict.ContainsKey(chosen)) dict[chosen] = 0;
            dict[chosen] += 1;
            slotFilledSoFar[slot] = filledInSlot + 1;
        }

        foreach (var kv in fillPerSlot)
        {
            if (kv.Value != 3) continue;

            int slot = kv.Key;
            if (!slotCounts.TryGetValue(slot, out var dict)) continue;

            foreach (var pair in dict)
            {
                if (pair.Value >= 3)
                    return false;
            }
        }

        return true;
    }

    private void ClearArrays(bool[] a, GameObject[] b)
    {
        for (int i = 0; i < a.Length; i++) a[i] = false;
        for (int i = 0; i < b.Length; i++) b[i] = null;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
