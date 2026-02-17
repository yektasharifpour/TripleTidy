using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class TimerBarHybrid : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI timerText;

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

    private Tween colorTween;
    private WinUIController winUIController;

    private Color currentTargetColor;
    private int lastDisplayedSeconds = -1;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        ResetTimer();
    }
    private void Start()
    {
        winUIController = FindObjectOfType<WinUIController>();
        StartTimer();   
    }

    private void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        float normalized = durationSeconds <= 0f ? 0f : (timeLeft / durationSeconds);

        UpdateFillAmount(normalized);

        UpdateColor(normalized);

        if (timeLeft <= 0f)
        {
            running = false;
            onTimeUp?.Invoke();
            if (winUIController != null)
                winUIController.lose();
        }
        setTextTimer(timeLeft);
    }

    private void UpdateColor(float normalized)
    {
        if (fillImage == null) return;

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
        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
            fillImage.color = greenColor;
        }
        currentTargetColor = greenColor;
        setTextTimer(timeLeft, true);
    }

    public float GetTimeLeft() => timeLeft;

    private void UpdateFillAmount(float normalized)
    {
        if (fillImage == null) return;

        if (smoothTime <= 0f)
        {
            fillImage.fillAmount = normalized;
            return;
        }

        float maxDelta = Time.deltaTime / smoothTime;
        fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, normalized, maxDelta);
    }

    private void  setTextTimer(float remainingTime, bool force = false)
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(remainingTime));
        if (!force && totalSeconds == lastDisplayedSeconds) return;
        lastDisplayedSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.SetText("{0} : {1:00}", minutes, seconds);
    }
 
}
