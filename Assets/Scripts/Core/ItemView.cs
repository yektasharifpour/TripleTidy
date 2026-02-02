using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;    

public class ItemView : MonoBehaviour
{
    [SerializeField] private ItemType type;
    [SerializeField] private TMP_Text label;

    public ItemType Type => type;

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    private void Awake()
    {
        UpdateVisual();
    }

    public void SetType(ItemType newType)
    {
        type = newType;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (label == null) return;

        label.text = type == ItemType.Ice ? "Ice" : "Choc";
    }
}
