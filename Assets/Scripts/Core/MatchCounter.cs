using UnityEngine;
using TMPro;
public class MatchCounter : MonoBehaviour
{
    

    [SerializeField] private TextMeshProUGUI matched_count;
    [SerializeField] private int matchedTotal;


    public void AddMatches(int count)
    {
        if (count <= 0) return;
        matchedTotal += count;
    }
    
    void Update()
    {
        matched_count.text = "combo :" + matchedTotal.ToString();
    }
}


