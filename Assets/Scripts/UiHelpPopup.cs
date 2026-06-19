using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Booth guide: builds a HELP button and scrollable popup that explains the UI.
/// Attach to the main screen Canvas (or any child); UI is created at runtime.
/// </summary>
public class UiHelpPopup : MonoBehaviour
{
    const string DefaultBody =
        "<b>TOP LEFT — RUN STATUS</b>\n" +
        "• <b>Customers</b> — how many patrons you still have. Win duels to gain one; lose to lose one. At zero, the run ends.\n" +
        "• <b>Energy</b> — your teller energy (0–100). Large wins can restore a little.\n\n" +
        "<b>TENT VIEW</b>\n" +
        "• <b>Call Client In</b> — floating button above the waiting patron. You must call them in before drawing cards.\n\n" +
        "<b>NAVIGATION (RIGHT SIDE)</b>\n" +
        "• <b>Tent</b> — return to the main booth view.\n" +
        "• <b>Book of Wisdom</b> — lore and scoring notes. Use <b>Previous Page</b> / <b>Next Page</b> to flip spreads.\n" +
        "• <b>Cards</b> — tarot table: draw your spread and write your reading.\n" +
        "• <b>Judge</b> — verdict screen after the spirit answers.\n\n" +
        "<b>CARDS PANEL</b>\n" +
        "• <b>Draw Cards</b> — pull three cards for this customer (once per round until you accept the verdict).\n" +
        "• <b>Reading box</b> — type your fortune. Echo each card’s vignette, name its theme (Greed / Vanity / Chaos / Power), lean toward hope, and speak to whether the customer is rich or poor.\n" +
        "• <b>Magical Energy</b> — duel slider (0–100). Committed when you press <b>Read Fortune</b>. At full power you win the duel automatically.\n" +
        "• <b>Read Fortune</b> — submit your reading and lock in magical energy for scoring.\n\n" +
        "<b>SPIRIT VIEW</b>\n" +
        "• Opens from the spirit camera after cards are drawn. Shows the demon’s curse once the model finishes.\n\n" +
        "<b>JUDGE PANEL</b>\n" +
        "• <b>Make Judgement</b> — scores your reading vs. the spirit (enabled after cards, your reading, and the spirit reply).\n" +
        "• Verdict text — explains who won and why.\n" +
        "• <b>Accept Judgement</b> — applies energy and customer changes, then clears the tent for the next patron.\n\n" +
        "<b>TYPICAL ROUND</b>\n" +
        "Call client in → Draw Cards → write reading → Read Fortune → wait for spirit → Judge → Make Judgement → Accept Judgement → repeat.";

    [Header("Optional overrides")]
    [SerializeField] RectTransform uiRoot;
    [SerializeField] Vector2 helpButtonPosition = new Vector2(-560f, 484f);
    [SerializeField] Vector2 helpButtonSize = new Vector2(90f, 90f);
    [SerializeField, TextArea(4, 24)] string helpBody = DefaultBody;

    GameObject _popupRoot;
    TMP_FontAsset _font;

    void Awake()
    {
        EnsureUiBuilt();
    }

    void Update()
    {
        if (_popupRoot != null && _popupRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    void EnsureUiBuilt()
    {
        if (uiRoot == null)
            uiRoot = transform as RectTransform;
        if (uiRoot == null)
        {
            Debug.LogWarning("[UiHelpPopup] Assign a RectTransform under the main Canvas.");
            return;
        }

        _font = ResolveFont();
        if (FindExistingHelpButton() == null)
            CreateHelpButton();
        if (_popupRoot == null)
            CreatePopup();
    }

    Button FindExistingHelpButton()
    {
        foreach (Transform child in uiRoot)
        {
            if (child.name == "HelpBtn" && child.TryGetComponent(out Button btn))
                return btn;
        }
        return null;
    }

    TMP_FontAsset ResolveFont()
    {
        var existing = Object.FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null)
            return existing.font;
        return TMP_Settings.defaultFontAsset;
    }

    void CreateHelpButton()
    {
        var btnGo = new GameObject("HelpBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(uiRoot, false);

        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = helpButtonPosition;
        rt.sizeDelta = helpButtonSize;

        var img = btnGo.GetComponent<Image>();
        img.sprite = LoadSprite("UI/UIBtn_Help");
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = true;

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Show);
    }

    void CreatePopup()
    {
        _popupRoot = new GameObject("HelpPopup", typeof(RectTransform), typeof(CanvasRenderer));
        _popupRoot.transform.SetParent(uiRoot, false);
        _popupRoot.SetActive(false);

        var rootRt = _popupRoot.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        dimGo.transform.SetParent(_popupRoot.transform, false);
        StretchFull(dimGo.GetComponent<RectTransform>());
        var dimImg = dimGo.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.62f);
        dimImg.raycastTarget = true;
        var dimBtn = dimGo.GetComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.onClick.AddListener(Hide);

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(_popupRoot.transform, false);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(720f, 820f);
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = LoadSprite("UI/UIPanel_HelpPopup");
        panelImg.color = Color.white;
        panelImg.preserveAspect = false;
        panelImg.type = Image.Type.Simple;

        CreateTitle(panelGo.transform, "BOOTH GUIDE");
        CreateScrollBody(panelGo.transform, string.IsNullOrWhiteSpace(helpBody) ? DefaultBody : helpBody);
        CreateCloseButton(panelGo.transform);
    }

    void CreateTitle(Transform parent, string title)
    {
        var go = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -18f);
        rt.sizeDelta = new Vector2(-48f, 52f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = title;
        tmp.fontSize = 30f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.91f, 0.78f, 0.42f);
        tmp.raycastTarget = false;
    }

    void CreateScrollBody(Transform parent, string body)
    {
        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(36f, 88f);
        scrollRt.offsetMax = new Vector2(-36f, -96f);
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        StretchFull(viewportGo.GetComponent<RectTransform>());
        var viewportImg = viewportGo.GetComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 1200f);

        var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(contentGo.transform, false);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        StretchFull(bodyRt);
        bodyRt.offsetMin = new Vector2(12f, 12f);
        bodyRt.offsetMax = new Vector2(-12f, -12f);

        var tmp = bodyGo.GetComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = body;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.96f, 0.9f, 0.78f);
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        tmp.raycastTarget = false;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewportGo.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        Canvas.ForceUpdateCanvases();
        float height = tmp.preferredHeight + 32f;
        contentRt.sizeDelta = new Vector2(0f, Mathf.Max(height, scrollRt.rect.height));
    }

    void CreateCloseButton(Transform parent)
    {
        var btnGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 22f);
        rt.sizeDelta = new Vector2(220f, 56f);

        var img = btnGo.GetComponent<Image>();
        img.color = new Color(0.36f, 0.13f, 0.18f, 0.95f);
        img.raycastTarget = true;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = "CLOSE";
        tmp.fontSize = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.96f, 0.9f, 0.78f);
        tmp.raycastTarget = false;

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Hide);
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static Sprite LoadSprite(string resourcesPath)
    {
        return Resources.Load<Sprite>(resourcesPath);
    }

    public void Show()
    {
        EnsureUiBuilt();
        if (_popupRoot != null)
            _popupRoot.SetActive(true);
    }

    public void Hide()
    {
        if (_popupRoot != null)
            _popupRoot.SetActive(false);
    }
}
