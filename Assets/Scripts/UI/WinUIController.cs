using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerBarHybrid timer;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private ParticleSystem confetti;

    [Header("Win Image Animation")]
    [SerializeField] private RectTransform winImage;
    [SerializeField] private float popDuration = 0.45f;
    [SerializeField] private float overshoot = 1.7f;
    [Tooltip("final scale")]
    [SerializeField] private Vector3 winfinalScale = Vector3.one;

    [Header("loseImage Animation")]
    [SerializeField] private RectTransform loseImage;
    [SerializeField] private float dropDuration = 0.55f;
    [Tooltip("final scale")]
    [SerializeField] private Vector3 losefinalScale = Vector3.one;





    private bool hasWon;

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);


        if (winImage != null && winfinalScale == Vector3.zero)
            winfinalScale = winImage.localScale;
        if (losePanel != null && losefinalScale == Vector3.zero)
            losefinalScale = loseImage.localScale;
    }

    public void Win()
    {
        if (hasWon) return;
        hasWon = true;

        if (timer != null) timer.PauseTimer();

        if (winPanel != null) winPanel.SetActive(true);

        if (winImage != null)
        {
            winImage.DOKill();

            var baseScale = new Vector3(winfinalScale.x, winfinalScale.y,winfinalScale.z);
            winImage.localScale = winfinalScale * 0.01f;

            winImage.DOScale(baseScale, popDuration)
                    .SetEase(Ease.OutBack, overshoot)
                    .SetUpdate(true);
        }


        if (confetti != null)
        {
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            confetti.Play();
        }
    }

    public void lose()
    {
        
        if (losePanel != null)
        {
             losePanel.SetActive(true);
            loseImage.DOKill();

            
            RectTransform canvasRT = loseImage.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            Vector2 centerPos = Vector2.zero;

            
            float canvasHalfHeight = canvasRT.rect.height * 0.5f;
            float imageHalfHeight = loseImage.rect.height * 0.5f;

            Vector2 startPos = new Vector2(
                0,
                canvasHalfHeight + imageHalfHeight + 50f 
            );

            loseImage.anchoredPosition = startPos;
            loseImage.localScale = losefinalScale;

            loseImage.DOAnchorPos(centerPos, dropDuration)
                    .SetEase(Ease.OutBack, overshoot)
                    .SetUpdate(true);
        
    }

        
    }
    public void restart()
    {
        SceneManager.LoadScene(0);
    }
}
