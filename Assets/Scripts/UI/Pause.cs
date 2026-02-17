using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class Pause : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;

    [Header("Timer")]
    [SerializeField] private TimerBarHybrid timer;

    private Button pauseButton;
    private bool paused;

    private void Awake()
    {
        pauseButton = GetComponent<Button>();

        if (timer == null)
            timer = FindObjectOfType<TimerBarHybrid>(true);

        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    private void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPausePressed);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumePressed);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuPressed);
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPausePressed);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResumePressed);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuPressed);
    }

    public void OnPausePressed()
    {
        if (paused) return;
        paused = true;

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        if (timer != null)
            timer.PauseTimer();
    }

    private void OnResumePressed()
    {
        if (!paused) return;
        paused = false;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (timer != null && timer.GetTimeLeft() > 0f)
            timer.StartTimer();
    }

    private void OnMenuPressed()
    {
        SceneManager.LoadScene(0);
    }
}
