using System.Collections;
using UnityEngine;

public class OpeningController : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup splashTitle;

    [Header("Duração em segundos")]
    [SerializeField, Min(0f)] private float fadeInDuration = 1f;
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;

    private IEnumerator Start()
    {
        if (splashPanel == null || mainMenuPanel == null || splashTitle == null)
        {
            Debug.LogError("OpeningController: conecte Splash Panel, Main Menu Panel e Splash Title no Inspector.", this);
            yield break;
        }

        Animation titleAnimation = splashTitle.GetComponent<Animation>();
        if (titleAnimation != null)
        {
            titleAnimation.Stop();
            titleAnimation.enabled = false;
        }

        Animator titleAnimator = splashTitle.GetComponent<Animator>();
        if (titleAnimator != null)
            titleAnimator.enabled = false;

        splashTitle.alpha = 0f;
        mainMenuPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        splashPanel.SetActive(true);

        yield return PlayOpening();
    }

    private IEnumerator PlayOpening()
    {
        yield return FadeTitle(0f, 1f, fadeInDuration);
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);
        yield return FadeTitle(1f, 0f, fadeOutDuration);
        ShowMainMenu();
    }

    private IEnumerator FadeTitle(float from, float to, float duration)
    {
        splashTitle.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            splashTitle.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
        }

        splashTitle.alpha = to;
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        splashPanel.SetActive(false);
    }
}
