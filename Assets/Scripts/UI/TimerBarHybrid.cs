using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TimerBarHybrid : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;

    [Header("Timer")]
    [SerializeField] private float durationSeconds = 30f;

    [Header("Tween")]
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float colorTweenTime = 0.2f;

    [Header("Colors")]
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;

    [Header("Thresholds")]
    [Range(0f, 1f)][SerializeField] private float yellowThreshold = 0.6f;
    [Range(0f, 1f)][SerializeField] private float redThreshold = 0.3f;

    [Header("Events")]
    public UnityEvent onTimeUp;

    private float timeLeft;
    private bool running;

    private Tween fillTween;
    private Tween colorTween;

    private Color currentTargetColor;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        ResetTimer();
    }
    private void Start()
    {
        StartTimer();   
    }

    private void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        float normalized = durationSeconds <= 0f ? 0f : (timeLeft / durationSeconds);

        fillTween?.Kill();
        fillTween = fillImage
            .DOFillAmount(normalized, smoothTime)
            .SetEase(Ease.Linear);

        UpdateColor(normalized);

        if (timeLeft <= 0f)
        {
            running = false;
            onTimeUp?.Invoke();
        }
    }

    private void UpdateColor(float normalized)
    {
        Color target;

        if (normalized > yellowThreshold)
            target = greenColor;
        else if (normalized > redThreshold)
            target = yellowColor;
        else
            target = redColor;

        if (target == currentTargetColor) return;

        currentTargetColor = target;

        colorTween?.Kill();
        colorTween = fillImage
            .DOColor(target, colorTweenTime)
            .SetEase(Ease.OutQuad);
    }

    public void StartTimer() => running = true;

    public void PauseTimer() => running = false;

    public void ResetTimer()
    {
        timeLeft = durationSeconds;
        fillImage.fillAmount = 1f;
        fillImage.color = greenColor;
        currentTargetColor = greenColor;
    }

    public float GetTimeLeft() => timeLeft;
}
