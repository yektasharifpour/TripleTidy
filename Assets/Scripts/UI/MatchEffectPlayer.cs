using DG.Tweening;
using UnityEngine;
using System;
public enum MatchEffectType
{
    ScaleDown,
    FadeOut,
    PopAndFade,
    FlyUpAndFade
}

[RequireComponent(typeof(CanvasGroup))]
public class MatchEffectPlayer : MonoBehaviour
{
    [SerializeField] private MatchEffectType effectType = MatchEffectType.ScaleDown;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private RectTransform rect;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PlayAndDestroy()
    {
        PlayAndDestroy(null);
    }

    public void PlayAndDestroy(Action onDestroyed)
    {
        var seq = DG.Tweening.DOTween.Sequence();

        switch (effectType)
        {
            case MatchEffectType.ScaleDown:
                seq.Append(rect.DOScale(0f, duration).SetEase(ease));
                break;

            case MatchEffectType.FadeOut:
                seq.Append(canvasGroup.DOFade(0f, duration));
                break;

            case MatchEffectType.PopAndFade:
                seq.Append(rect.DOScale(1.2f, duration * 0.4f).SetEase(Ease.OutBack));
                seq.Append(rect.DOScale(0f, duration * 0.6f).SetEase(Ease.InBack));
                seq.Join(canvasGroup.DOFade(0f, duration));
                break;

            case MatchEffectType.FlyUpAndFade:
                seq.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + 80f, duration));
                seq.Join(canvasGroup.DOFade(0f, duration));
                break;
        }

        seq.OnComplete(() =>
        {
            Destroy(gameObject);

            onDestroyed?.Invoke();
        });
    }
}
