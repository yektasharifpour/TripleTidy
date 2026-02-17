using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FreezePowerUp : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Maximum number of times this power-up can be used per game")]
    [SerializeField] private int maxUses = 3;

    [Tooltip("Cooldown in seconds between uses")]
    [SerializeField] private float cooldownSeconds = 10f;

    [Tooltip("How long to freeze the timer (seconds)")]
    [SerializeField] private float freezeDurationSeconds = 5f;

    [Header("References")]
    [SerializeField] private TimerBarHybrid timer;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem freezeEffect;
    [SerializeField] private AudioClip freezeSound;

    private Button button;
    private int remainingUses;
    private float lastUsedTime = -999f;
    private AudioSource audioSource;

    private bool isFreezing = false;
    private float freezeEndUnscaledTime = 0f;
    private bool resumeAfterFreeze = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        remainingUses = maxUses;

        if (timer == null)
            timer = FindObjectOfType<TimerBarHybrid>(true);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && freezeSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        UpdateButtonState();
    }

    private void Start()
    {
        button.onClick.AddListener(ActivateFreeze);
    }

    private void Update()
    {
        if (isFreezing && Time.unscaledTime >= freezeEndUnscaledTime)
        {
            isFreezing = false;

            if (resumeAfterFreeze && timer != null && timer.GetTimeLeft() > 0f)
                timer.StartTimer();
        }

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (button == null) return;

        if (remainingUses <= 0)
        {
            button.interactable = false;
            return;
        }

        if (isFreezing)
        {
            button.interactable = false;
            return;
        }

        bool onCooldown = Time.time - lastUsedTime < cooldownSeconds;
        button.interactable = !onCooldown;
    }

    public void ActivateFreeze()
    {
        if (remainingUses <= 0) return;
        if (isFreezing) return;
        if (Time.time - lastUsedTime < cooldownSeconds) return;

        if (timer == null)
            timer = FindObjectOfType<TimerBarHybrid>(true);
        if (timer == null) return;

        resumeAfterFreeze = timer.GetTimeLeft() > 0f;
        timer.PauseTimer();

        isFreezing = true;
        freezeEndUnscaledTime = Time.unscaledTime + Mathf.Max(0f, freezeDurationSeconds);

        if (freezeEffect != null)
        {
            var effect = Instantiate(freezeEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }

        if (audioSource != null && freezeSound != null)
        {
            audioSource.PlayOneShot(freezeSound);
        }

        remainingUses--;
        lastUsedTime = Time.time;
        UpdateButtonState();
    }

    public void ResetPowerUp()
    {
        remainingUses = maxUses;
        lastUsedTime = -999f;
        isFreezing = false;
        UpdateButtonState();
    }
}
