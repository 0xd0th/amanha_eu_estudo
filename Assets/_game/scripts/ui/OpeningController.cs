using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class OpeningController : MonoBehaviour
{
    [Header("Vídeo de introdução")]
    [SerializeField] private VideoClip introClip;
    [SerializeField, Min(0f)] private float introDuration = 6f;
    [SerializeField, Min(0f)] private float blackScreenDuration = 1f;
    [SerializeField, Range(0.1f, 1f)] private float introPlaybackSpeed = 1f;
    private GameObject introOverlay;
    private VideoPlayer introPlayer;
    private bool introFailed;

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

        var developmentOptions = splashPanel.GetComponentInParent<OpeningDevelopmentOptions>();
        if (developmentOptions != null && developmentOptions.SkipOpening)
        {
            ShowMainMenu();
            yield break;
        }

        yield return PlayIntro();
        yield return PlayOpening();
    }

    private IEnumerator PlayIntro()
    {
        if (introClip == null || introDuration <= 0f)
            yield break;

        introOverlay = new GameObject("IntroVideo", typeof(RectTransform), typeof(Image));
        var rect = introOverlay.GetComponent<RectTransform>();
        rect.SetParent(splashPanel.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        introOverlay.GetComponent<Image>().color = Color.black;

        var picture = new GameObject("VideoImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        picture.transform.SetParent(rect, false);
        var image = picture.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.color = Color.clear;
        var fit = picture.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = introClip.height > 0 ? (float)introClip.width / introClip.height : 16f / 9f;

        introPlayer = introOverlay.AddComponent<VideoPlayer>();
        introPlayer.playOnAwake = false;
        introPlayer.isLooping = false;
        introPlayer.waitForFirstFrame = true;
        introPlayer.renderMode = VideoRenderMode.APIOnly;
        introPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        introPlayer.clip = introClip;
        introPlayer.errorReceived += OnVideoError;
        introFailed = false;
        introPlayer.Prepare();
        if (blackScreenDuration > 0f)
            yield return new WaitForSecondsRealtime(blackScreenDuration);
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!introPlayer.isPrepared && !introFailed && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (introPlayer.isPrepared && !introFailed)
        {
            if (introPlayer.canSetPlaybackSpeed)
                introPlayer.playbackSpeed = introPlaybackSpeed;
            else
                Debug.LogWarning("Este dispositivo não permite alterar a velocidade do vídeo.", this);
            introPlayer.Play();
            float elapsed = 0f;
            while (!introFailed && elapsed < introDuration)
            {
                if (introPlayer.texture != null)
                {
                    image.texture = introPlayer.texture;
                    image.color = Color.white;
                }
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
        }
        else if (!introFailed)
            Debug.LogWarning("O vídeo não ficou pronto a tempo. Continuando para o título.", this);

        CleanupIntro();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        introFailed = true;
        Debug.LogWarning("Falha no vídeo de introdução: " + message, this);
    }

    private void CleanupIntro()
    {
        if (introPlayer != null)
        {
            introPlayer.errorReceived -= OnVideoError;
            introPlayer.Stop();
        }
        if (introOverlay != null)
            Destroy(introOverlay);
        introPlayer = null;
        introOverlay = null;
    }

    private void OnDestroy() => CleanupIntro();

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
