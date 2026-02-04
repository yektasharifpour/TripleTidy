using UnityEngine;
using TMPro;
using DG.Tweening;

public class MatchCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI matchedCountText;
    [SerializeField] private int matchedTotal;

    [Header("Pop Effect")]
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popDuration = 0.15f;

    private Vector3 baseScale;
    private Tween popTween;

    private void Awake()
    {
        if (matchedCountText == null)
            matchedCountText = GetComponent<TextMeshProUGUI>();

        baseScale = matchedCountText.transform.localScale;
        UpdateText();
    }

    public void AddMatches(int count)
    {
        if (count <= 0) return;

        matchedTotal += count;
        UpdateText();
        PlayPopEffect();
    }

    private void UpdateText()
    {
        matchedCountText.text = $"combo x{matchedTotal}";
    }

    private void PlayPopEffect()
    {
        popTween?.Kill();

        var t = matchedCountText.transform;

        t.localScale = baseScale;
        popTween = t.DOScale(baseScale * popScale, popDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
                t.DOScale(baseScale, popDuration * 0.9f)
                 .SetEase(Ease.InBack)
            );

        t.DOPunchRotation(
            new Vector3(0, 0, 6f), 
            0.25f,                 
            10,                    
            1f                     
        );
        matchedCountText
    .DOColor(Color.yellow, 0.1f)
    .OnComplete(() =>
        matchedCountText.DOColor(Color.white, 0.1f)
    );
    }
}
