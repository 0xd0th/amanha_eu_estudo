using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NewGameCutsceneController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Font font;
    [SerializeField] private Texture2D clockArtwork;

    [Header("Roteiro")]
    [SerializeField] private string title = "14 DIAS ANTES...";
    [SerializeField, TextArea(2, 5)] private string[] lines = {
        "2 SEMANAS PRAS PROVAS...",
        "MINHA NAMORADA TERMINOU\nCOMIGO...",
        "FICAR PENSANDO NO\nPASSADO NÃO VAI MUDAR\nNADA. MELHOR SEGUIR A VIDA"
    };
    [SerializeField] private Color textColor = new Color(1f, .9f, 0f);
    [SerializeField, Min(0)] private float titleDuration = 2.5f;
    [SerializeField, Min(0)] private float clockDuration = 1.2f;
    [SerializeField, Min(0)] private float charactersPerSecond = 35f;

    [Header("Conclusão — próxima cena")]
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();

    public enum CutsceneStage { Idle, Title, Clock, Dialogue, Completed }
    public CutsceneStage Stage { get; private set; }
    public int LineIndex { get; private set; }
    private GameObject presentation;
    private GameObject cutscenePanel;
    private RawImage clockImage;
    private Text titleText;
    private Text dialogueText;
    private Text hintText;
    private Button advanceButton;
    private bool loadingNextScene;
    private float elapsed;
    private float revealed;

    public void BeginNewGame()
    {
        // Ignore repeated clicks while a new game is already being presented.
        if (Stage != CutsceneStage.Idle) return;
        if (font == null || clockArtwork == null || mainMenuPanel == null)
        {
            Debug.LogError("Conecte menu, fonte, arte do despertador na cutscene.", this);
            return;
        }
        BuildPresentation();
        mainMenuPanel.SetActive(false);
        Stage = CutsceneStage.Title;
        elapsed = 0f;
        titleText.gameObject.SetActive(true);
        clockImage.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(false);
        hintText.gameObject.SetActive(false);
        advanceButton.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (Stage == CutsceneStage.Title && elapsed >= titleDuration)
        {
            titleText.gameObject.SetActive(false);
            clockImage.gameObject.SetActive(true);
            Stage = CutsceneStage.Clock;
            elapsed = 0f;
        }
        else if (Stage == CutsceneStage.Clock && elapsed >= clockDuration)
        {
            LineIndex = 0;
            ShowLine();
        }
        else if (Stage == CutsceneStage.Dialogue)
        {
            revealed = charactersPerSecond <= 0 ? CurrentLine.Length : revealed + Time.unscaledDeltaTime * charactersPerSecond;
            UpdateLineText();
        }
    }

    private string CurrentLine => lines[LineIndex] ?? "";

    private void ShowLine()
    {
        if (lines == null || LineIndex >= lines.Length) { Complete(); return; }
        Stage = CutsceneStage.Dialogue;
        revealed = 0f;
        dialogueText.gameObject.SetActive(true);
        hintText.gameObject.SetActive(true);
        advanceButton.interactable = true;
        UpdateLineText();
        advanceButton.Select();
    }

    private void UpdateLineText()
    {
        int count = Mathf.Clamp(Mathf.FloorToInt(revealed), 0, CurrentLine.Length);
        dialogueText.text = CurrentLine.Substring(0, count);
        hintText.text = count < CurrentLine.Length ? "CLIQUE OU ENTER PARA REVELAR" : "CLIQUE OU ENTER PARA CONTINUAR";
    }

    public void Advance()
    {
        if (Stage != CutsceneStage.Dialogue) return;
        if (revealed < CurrentLine.Length)
        {
            revealed = CurrentLine.Length;
            UpdateLineText();
            return;
        }
        LineIndex++;
        ShowLine();
    }

    private void Complete()
    {
        if (Stage == CutsceneStage.Completed) return;
        Stage = CutsceneStage.Completed;
        advanceButton.interactable = false;
        cutscenePanel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        // Keep the black background until the host transitions to the next scene.
        onCompleted.Invoke();
    }

    public void LoadNextScene(string sceneName)
    {
        if (Stage != CutsceneStage.Completed || loadingNextScene) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Cena de continuação ausente no Build Profile: " + sceneName, this);
            return;
        }
        loadingNextScene = true;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    private void BuildPresentation()
    {
        presentation = new GameObject("NewGamePresentation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        presentation.transform.SetParent(transform, false);
        var canvas = presentation.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = presentation.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var background = Box("BlackBackground", presentation.transform, Color.black);
        Stretch(background.rectTransform);
        var frame = Rect("Composition", presentation.transform, new Vector2(1920, 1080), Vector2.zero);

        cutscenePanel = Rect("CutscenePanel", frame, new Vector2(1920, 1080), Vector2.zero).gameObject;
        clockImage = Picture("ClockArtwork", cutscenePanel.transform, clockArtwork, new Vector2(1920, 1080));
        titleText = Label("TimeTitle", cutscenePanel.transform, title, 56, new Vector2(1700, 160), Vector2.zero, TextAnchor.MiddleCenter);
        dialogueText = Label("DialogueText", cutscenePanel.transform, "", 48, new Vector2(1680, 350), new Vector2(0, 280), TextAnchor.UpperLeft);
        var advanceImage = Box("AdvanceButton", cutscenePanel.transform, Color.clear);
        Stretch(advanceImage.rectTransform);
        advanceButton = advanceImage.gameObject.AddComponent<Button>();
        advanceButton.targetGraphic = advanceImage;
        advanceButton.transition = Selectable.Transition.None;
        advanceButton.onClick.AddListener(Advance);
        hintText = Label("AdvanceHint", cutscenePanel.transform, "", 18, new Vector2(850, 40), new Vector2(-415, -160), TextAnchor.MiddleLeft);
        hintText.color = new Color(.8f, .8f, .8f);

    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image Box(string name, Transform parent, Color color)
    {
        var image = Rect(name, parent, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static RawImage Picture(string name, Transform parent, Texture texture, Vector2 size)
    {
        var image = Rect(name, parent, size, Vector2.zero).gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        var fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)texture.width / texture.height;
        return image;
    }

    private Text Label(string name, Transform parent, string value, int size, Vector2 dimensions, Vector2 position, TextAnchor alignment)
    {
        var text = Rect(name, parent, dimensions, position).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = textColor;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.supportRichText = false;
        return text;
    }
}
