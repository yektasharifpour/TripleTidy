using UnityEngine;

public class MatchCounter : MonoBehaviour
{
    private static int matchedTotal;

    [SerializeField] private Vector2 position = new Vector2(16f, 16f);
    [SerializeField] private int fontSize = 19;
    [SerializeField] private Color textColor = Color.white;

    public static void AddMatches(int count)
    {
        if (count <= 0) return;
        matchedTotal += count;
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            normal = { textColor = textColor }
        };

        GUI.Label(new Rect(position.x, position.y, 300f, 40f), $"{matchedTotal}", style);
    }
}
