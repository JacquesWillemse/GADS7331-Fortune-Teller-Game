using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen carnival UI at run start: pick fortune-telling lineage (1–3) before the tent opens.
/// </summary>
[DefaultExecutionOrder(-80)]
public class ExperienceSelectOverlay : MonoBehaviour
{
    const float PanelWidth = 900f;
    const float PanelHeight = 980f;
    const float ChoiceHeight = 158f;
    const float ChoiceGap = 14f;

    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private GameManager gameManager;

    GameObject _overlayRoot;
    TMP_FontAsset _font;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (uiRoot == null)
        {
            var canvas = GetComponent<RectTransform>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>()?.transform as RectTransform;
            uiRoot = canvas;
        }

        BuildOverlay();
        Show();
    }

    void BuildOverlay()
    {
        if (uiRoot == null || _overlayRoot != null)
            return;

        _font = ResolveFont();

        _overlayRoot = new GameObject("ExperienceSelectOverlay", typeof(RectTransform), typeof(CanvasRenderer));
        _overlayRoot.transform.SetParent(uiRoot, false);
        StretchFull(_overlayRoot.GetComponent<RectTransform>());

        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimGo.transform.SetParent(_overlayRoot.transform, false);
        StretchFull(dimGo.GetComponent<RectTransform>());
        dimGo.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.06f, 0.92f);

        Transform panel = CreatePanel(_overlayRoot.transform);
        CreateTitle(panel, "HOW DEEP RUNS YOUR LINEAGE?");

        var introTmp = CreateBody(
            panel,
            "Before the Trans Balkan Express opens its flaps, the booth asks what you still remember of fortune-telling.\n\n" +
            "Your answer steers the judge, your starting <b>energy</b>, how many pasteboards the <b>Book of Wisdom</b> reveals, " +
            "and how much the <b>bound spirit</b> already knows about each card's theme and moral lean.");

        Canvas.ForceUpdateCanvases();
        float introBottomY = LayoutIntro(introTmp);

        float y = introBottomY - ChoiceGap;
        CreateChoiceButton(panel, RunExperienceConfig.FortuneLineage.Novice, ref y);
        CreateChoiceButton(panel, RunExperienceConfig.FortuneLineage.Familiar, ref y);
        CreateChoiceButton(panel, RunExperienceConfig.FortuneLineage.Veteran, ref y);

        _overlayRoot.transform.SetAsLastSibling();
    }

    static Transform CreatePanel(Transform parent)
    {
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = null;
        panelImg.color = new Color(0.22f, 0.08f, 0.12f, 0.98f);

        var borderGo = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderGo.transform.SetParent(panelGo.transform, false);
        var borderRt = borderGo.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(6f, 6f);
        borderRt.offsetMax = new Vector2(-6f, -6f);
        var borderImg = borderGo.GetComponent<Image>();
        borderImg.sprite = null;
        borderImg.color = new Color(0.88f, 0.72f, 0.38f, 0.35f);
        borderImg.raycastTarget = false;

        var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        innerGo.transform.SetParent(panelGo.transform, false);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(14f, 14f);
        innerRt.offsetMax = new Vector2(-14f, -14f);
        var innerImg = innerGo.GetComponent<Image>();
        innerImg.sprite = null;
        innerImg.color = new Color(0.18f, 0.06f, 0.1f, 0.92f);
        innerImg.raycastTarget = false;

        return panelGo.transform;
    }

    void CreateTitle(Transform parent, string title)
    {
        var go = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -28f);
        rt.sizeDelta = new Vector2(-80f, 72f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = title;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.91f, 0.78f, 0.42f);
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
    }

    TextMeshProUGUI CreateBody(Transform parent, string body)
    {
        var go = new GameObject("Intro", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -108f);
        rt.sizeDelta = new Vector2(-96f, 220f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.text = body;
        tmp.fontSize = 19f;
        tmp.lineSpacing = 2f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.96f, 0.9f, 0.78f);
        tmp.richText = true;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    static float LayoutIntro(TextMeshProUGUI introTmp)
    {
        if (introTmp == null)
            return -320f;

        var rt = introTmp.rectTransform;
        float height = Mathf.Max(introTmp.preferredHeight + 8f, 120f);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        return rt.anchoredPosition.y - height;
    }

    void CreateChoiceButton(Transform parent, RunExperienceConfig.FortuneLineage lineage, ref float y)
    {
        int level = (int)lineage;
        var btnGo = new GameObject("Choice" + level, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(780f, ChoiceHeight);
        y -= ChoiceHeight + ChoiceGap;

        var img = btnGo.GetComponent<Image>();
        img.color = new Color(0.32f, 0.12f, 0.16f, 0.98f);
        img.raycastTarget = true;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(16f, 10f);
        labelRt.offsetMax = new Vector2(-16f, -10f);

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.font = _font;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.96f, 0.9f, 0.78f);
        tmp.fontSize = 18f;
        tmp.lineSpacing = -2f;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.text =
            $"<b>{level}. {RunExperienceConfig.LineageTitle(lineage)}</b>\n" +
            $"{RunExperienceConfig.LineageBlurb(lineage)}\n" +
            $"<size=16><color=#E8C86A>{RunExperienceConfig.LineageStats(lineage)}</color></size>";

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = img.color;
        colors.highlightedColor = new Color(0.42f, 0.16f, 0.2f, 1f);
        colors.pressedColor = new Color(0.22f, 0.08f, 0.1f, 1f);
        btn.colors = colors;

        RunExperienceConfig.FortuneLineage captured = lineage;
        btn.onClick.AddListener(() => OnLineageChosen(captured));
    }

    void OnLineageChosen(RunExperienceConfig.FortuneLineage lineage)
    {
        RunExperienceConfig.Configure(lineage);
        if (gameManager != null)
            gameManager.ApplyExperienceStartingState();
        else
            Debug.LogWarning("[ExperienceSelectOverlay] GameManager not found — run config not applied.");

        Hide();
    }

    void Show()
    {
        if (_overlayRoot != null)
            _overlayRoot.SetActive(true);
    }

    void Hide()
    {
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
    }

    static TMP_FontAsset ResolveFont()
    {
        var existing = Object.FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (existing != null && existing.font != null)
            return existing.font;
        return TMP_Settings.defaultFontAsset;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
