using UnityEngine;
using TMPro;

public class WinUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerBarHybrid timer;           
    [SerializeField] private GameObject winPanel;            
    [SerializeField] private ParticleSystem confetti;       

    [Header("Optional")]
    [SerializeField] private TMP_Text winText;              

    private bool hasWon;

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
    }

    public void Win()
    {
        if (hasWon) return;
        hasWon = true;

        if (timer != null) timer.PauseTimer();

        if (winPanel != null) winPanel.SetActive(true);
        if (winText != null) winText.text = "Congratulation";

        if (confetti != null)
        {
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            confetti.Play();
        }

    }
}
