using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookManager : MonoBehaviour
{
    const string LockedBody =
        "Draw this card in the tent and name its <b>theme</b> in a reading to reveal its morality and full lore.";

    [SerializeField] private BookPages bookPages;
    [SerializeField] private TarotCards tarotDatabase;
    [SerializeField] private WisdomBookProgress wisdomBookProgress;

    [SerializeField] private TMP_Text leftPageTitle;
    [SerializeField] private TMP_Text leftPageBody;
    [SerializeField] private TMP_Text rightPageTitle;
    [SerializeField] private TMP_Text rightPageBody;
    [SerializeField] private RectTransform cardImageAnchor;

    [SerializeField] private Button previousPage;
    [SerializeField] private Button nextPage;

    [Header("Card border (matches tent spread)")]
    [SerializeField] private Texture2D tarotCardBorderTexture;

    [Header("Card page layout (book canvas space)")]
    [SerializeField] private Vector2 leftPageCenter = new Vector2(-370f, 0f);
    [SerializeField] private Vector2 rightPageCenter = new Vector2(390f, 0f);
    [SerializeField] private Vector2 cardImageSize = new Vector2(340f, 480f);
    [SerializeField] private float cardTitleCenterY = 440f;
    [SerializeField] private float cardLayoutGap = 18f;
    [SerializeField] private float cardBorderBottomPadding = 52f;
    [SerializeField] private Vector2 cardBodySize = new Vector2(580f, 200f);

    const float RefCardWidth = 400f;
    const float RefCardHeight = 630f;
    const float RefImageWidth = 300f;
    const float RefImageHeight = 400f;
    const float RefImageOffsetY = 67f;

    RectTransform _leftCardSlot;
    RectTransform _rightCardSlot;
    RectTransform _leftCardFrame;
    RectTransform _rightCardFrame;
    RawImage _leftCardBorder;
    RawImage _rightCardBorder;
    Image _leftCardImage;
    Image _rightCardImage;
    AspectRatioFitter _leftImageFitter;
    AspectRatioFitter _rightImageFitter;
    int _navIndex;

    Vector2 _leftTitlePos;
    Vector2 _leftTitleSize;
    Vector2 _rightTitlePos;
    Vector2 _rightTitleSize;
    Vector2 _leftBodyPos;
    Vector2 _leftBodySize;
    Vector2 _rightBodyPos;
    Vector2 _rightBodySize;

    void Awake()
    {
        CacheBodyLayout();
        EnsureCardImages();
    }

    void Start()
    {
        if (nextPage != null)
            nextPage.onClick.AddListener(OnNextPage);
        if (previousPage != null)
            previousPage.onClick.AddListener(OnPreviousPage);
        if (wisdomBookProgress != null)
            wisdomBookProgress.onCardRevealed.AddListener(OnCardRevealed);
        _navIndex = 0;
        RefreshView();
    }

    void OnDestroy()
    {
        if (nextPage != null)
            nextPage.onClick.RemoveListener(OnNextPage);
        if (previousPage != null)
            previousPage.onClick.RemoveListener(OnPreviousPage);
        if (wisdomBookProgress != null)
            wisdomBookProgress.onCardRevealed.RemoveListener(OnCardRevealed);
    }

    void CacheBodyLayout()
    {
        if (leftPageTitle != null)
        {
            var rt = leftPageTitle.rectTransform;
            _leftTitlePos = rt.anchoredPosition;
            _leftTitleSize = rt.sizeDelta;
        }

        if (rightPageTitle != null)
        {
            var rt = rightPageTitle.rectTransform;
            _rightTitlePos = rt.anchoredPosition;
            _rightTitleSize = rt.sizeDelta;
        }

        if (leftPageBody != null)
        {
            var rt = leftPageBody.rectTransform;
            _leftBodyPos = rt.anchoredPosition;
            _leftBodySize = rt.sizeDelta;
        }

        if (rightPageBody != null)
        {
            var rt = rightPageBody.rectTransform;
            _rightBodyPos = rt.anchoredPosition;
            _rightBodySize = rt.sizeDelta;
        }
    }

    void OnCardRevealed(int _)
    {
        RefreshView();
    }

    void EnsureCardImages()
    {
        Transform parent = cardImageAnchor != null ? cardImageAnchor : transform;
        EnsureSideSlot(
            parent,
            "WisdomCardSlotLeft",
            "WisdomCardImageLeft",
            out _leftCardSlot,
            out _leftCardFrame,
            out _leftCardBorder,
            out _leftCardImage,
            out _leftImageFitter);
        EnsureSideSlot(
            parent,
            "WisdomCardSlotRight",
            "WisdomCardImageRight",
            out _rightCardSlot,
            out _rightCardFrame,
            out _rightCardBorder,
            out _rightCardImage,
            out _rightImageFitter);
        ApplyCardImageLayout(cardTitleCenterY - TitleHalfHeight() - cardLayoutGap - BorderSize.y * 0.5f);
    }

    Vector2 BorderSize =>
        new Vector2(
            cardImageSize.x + SidePadding * 2f,
            cardImageSize.y + TopPadding + cardBorderBottomPadding);

    float SidePadding =>
        cardImageSize.x * (RefCardWidth - RefImageWidth) / RefImageWidth * 0.5f;

    float TopPadding =>
        cardImageSize.y * (RefCardHeight * 0.5f - RefImageOffsetY - RefImageHeight * 0.5f) / RefImageHeight;

    float FullTentBorderHeight => cardImageSize.y * (RefCardHeight / RefImageHeight);

    float FrameCenterYOffset =>
        cardBorderBottomPadding + cardImageSize.y * 0.5f - BorderSize.y * 0.5f;

    void EnsureSideSlot(
        Transform parent,
        string slotName,
        string imageName,
        out RectTransform slot,
        out RectTransform frame,
        out RawImage border,
        out Image image,
        out AspectRatioFitter fitter)
    {
        string frameName = imageName + "Frame";
        Transform slotTransform = parent.Find(slotName);
        if (slotTransform == null)
        {
            RemoveLegacyCardObjects(parent, slotName, imageName, frameName);

            var slotGo = new GameObject(slotName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            slotGo.transform.SetParent(parent, false);
            slotTransform = slotGo.transform;

            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = slotRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);

            border = slotGo.GetComponent<RawImage>();
            border.raycastTarget = false;
            ApplyBorderTexture(border);

            var frameGo = new GameObject(frameName, typeof(RectTransform), typeof(RectMask2D));
            frameGo.transform.SetParent(slotTransform, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = cardImageSize;

            var imageGo = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            imageGo.transform.SetParent(frameGo.transform, false);
            var imageRt = imageGo.GetComponent<RectTransform>();
            imageRt.anchorMin = Vector2.zero;
            imageRt.anchorMax = Vector2.one;
            imageRt.offsetMin = Vector2.zero;
            imageRt.offsetMax = Vector2.zero;
        }
        else
        {
            border = slotTransform.GetComponent<RawImage>();
            ApplyBorderTexture(border);
        }

        slot = slotTransform as RectTransform;
        frame = slotTransform.Find(frameName) as RectTransform;
        image = frame != null ? frame.Find(imageName)?.GetComponent<Image>() : null;
        fitter = image != null ? image.GetComponent<AspectRatioFitter>() : null;

        if (frame != null)
        {
            frame.sizeDelta = cardImageSize;
            frame.anchoredPosition = new Vector2(0f, FrameCenterYOffset);
        }

        if (image != null)
        {
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        if (fitter != null)
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
    }

    void ApplyBorderTexture(RawImage border)
    {
        if (border == null || tarotCardBorderTexture == null)
            return;
        border.texture = tarotCardBorderTexture;
        border.color = Color.white;
        float uvHeight = Mathf.Clamp01(BorderSize.y / FullTentBorderHeight);
        border.uvRect = new Rect(0f, 1f - uvHeight, 1f, uvHeight);
    }

    static void RemoveLegacyCardObjects(Transform parent, string slotName, string imageName, string frameName)
    {
        Transform legacyFrame = parent.Find(frameName);
        if (legacyFrame != null && legacyFrame.parent == parent)
            Object.Destroy(legacyFrame.gameObject);

        Transform legacyImage = parent.Find(imageName);
        if (legacyImage != null && legacyImage.parent == parent)
            Object.Destroy(legacyImage.gameObject);

        Transform legacySlot = parent.Find(slotName);
        if (legacySlot != null && legacySlot.parent == parent)
            Object.Destroy(legacySlot.gameObject);
    }

    int CardCount => tarotDatabase?.cards?.Count ?? 0;

    const int IntroSpreadCount = 2;

    int CardSpreadCount => CardCount <= 0 ? 0 : (CardCount + 1) / 2;

    /// <summary>Nav 0..IntroSpreadCount-1 = intro spreads; then card pair spreads.</summary>
    int LastNavIndex => IntroSpreadCount - 1 + CardSpreadCount;

    void OnNextPage()
    {
        if (_navIndex < LastNavIndex)
        {
            _navIndex++;
            RefreshView();
        }
    }

    void OnPreviousPage()
    {
        if (_navIndex > 0)
        {
            _navIndex--;
            RefreshView();
        }
    }

    void RefreshView()
    {
        if (_navIndex < IntroSpreadCount)
            ShowIntroSpread(_navIndex);
        else
            ShowCardSpread(_navIndex - IntroSpreadCount);

        if (previousPage != null)
            previousPage.interactable = _navIndex > 0;
        if (nextPage != null)
            nextPage.interactable = _navIndex < LastNavIndex;
    }

    void ShowIntroSpread(int introIndex)
    {
        SetCardImagesVisible(false);
        ApplyIntroBodyLayout();
        var pages = bookPages?.pages;
        int leftIndex = introIndex * 2;
        int rightIndex = leftIndex + 1;
        ApplyTextPage(leftPageTitle, leftPageBody, pages, leftIndex);
        ApplyTextPage(rightPageTitle, rightPageBody, pages, rightIndex);
    }

    void ShowCardSpread(int pairIndex)
    {
        ApplyCardBodyLayout();
        int leftCardIndex = pairIndex * 2;
        int rightCardIndex = leftCardIndex + 1;
        ApplyCardSide(leftPageTitle, leftPageBody, _leftCardImage, leftCardIndex);
        ApplyCardSide(rightPageTitle, rightPageBody, _rightCardImage, rightCardIndex);
    }

    void ApplyCardSide(TMP_Text titleTmp, TMP_Text bodyTmp, Image image, int cardIndex)
    {
        if (tarotDatabase?.cards == null || cardIndex < 0 || cardIndex >= tarotDatabase.cards.Count)
        {
            ClearTmp(titleTmp);
            ClearTmp(bodyTmp);
            SetSideImage(image, GetFitterForImage(image), GetSlotForImage(image), null, false, false);
            return;
        }

        TarotCardData card = tarotDatabase.cards[cardIndex];
        bool revealed = wisdomBookProgress != null && wisdomBookProgress.IsRevealed(cardIndex);
        string cardName = card.cardName?.Trim() ?? "";
        AspectRatioFitter fitter = GetFitterForImage(image);
        RectTransform slot = GetSlotForImage(image);

        SetTmp(titleTmp, cardName);
        ConfigureCardTitle(titleTmp);

        if (revealed)
        {
            SetTmp(bodyTmp, FormatRevealedCardBody(card));
            SetSideImage(image, fitter, slot, card.tarotCardImage, true, false);
        }
        else
        {
            SetTmp(bodyTmp, LockedBody);
            SetSideImage(image, fitter, slot, card.tarotCardImage, true, true);
        }
    }

    AspectRatioFitter GetFitterForImage(Image image)
    {
        if (image == _leftCardImage)
            return _leftImageFitter;
        if (image == _rightCardImage)
            return _rightImageFitter;
        return image != null ? image.GetComponent<AspectRatioFitter>() : null;
    }

    RectTransform GetSlotForImage(Image image)
    {
        if (image == _leftCardImage)
            return _leftCardSlot;
        if (image == _rightCardImage)
            return _rightCardSlot;
        return image != null ? image.transform.parent?.parent as RectTransform : null;
    }

    static void ConfigureCardTitle(TMP_Text titleTmp)
    {
        if (titleTmp == null)
            return;
        titleTmp.enableAutoSizing = true;
        titleTmp.fontSizeMin = 18f;
        titleTmp.fontSizeMax = 32f;
        titleTmp.textWrappingMode = TextWrappingModes.Normal;
    }

    void ApplyIntroBodyLayout()
    {
        ApplyTextRect(leftPageTitle, _leftTitlePos, _leftTitleSize);
        ApplyTextRect(rightPageTitle, _rightTitlePos, _rightTitleSize);
        ApplyBodyRect(leftPageBody, _leftBodyPos, _leftBodySize);
        ApplyBodyRect(rightPageBody, _rightBodyPos, _rightBodySize);
    }

    void ApplyCardBodyLayout()
    {
        float titleHalfHeight = TitleHalfHeight();
        float slotCenterY = cardTitleCenterY - titleHalfHeight - cardLayoutGap - BorderSize.y * 0.5f;
        float bodyCenterY = slotCenterY - BorderSize.y * 0.5f - cardLayoutGap - cardBodySize.y * 0.5f;

        ApplyTextRect(leftPageTitle, new Vector2(leftPageCenter.x, cardTitleCenterY), _leftTitleSize);
        ApplyTextRect(rightPageTitle, new Vector2(rightPageCenter.x, cardTitleCenterY), _rightTitleSize);
        ApplyBodyRect(leftPageBody, new Vector2(leftPageCenter.x, bodyCenterY), cardBodySize);
        ApplyBodyRect(rightPageBody, new Vector2(rightPageCenter.x, bodyCenterY), cardBodySize);
        ApplyCardImageLayout(slotCenterY);
        BringCardTextToFront();
    }

    float TitleHalfHeight()
    {
        if (leftPageTitle != null)
            return leftPageTitle.rectTransform.sizeDelta.y * 0.5f;
        if (rightPageTitle != null)
            return rightPageTitle.rectTransform.sizeDelta.y * 0.5f;
        return 50f;
    }

    void BringCardTextToFront()
    {
        if (leftPageTitle != null)
            leftPageTitle.transform.SetAsLastSibling();
        if (rightPageTitle != null)
            rightPageTitle.transform.SetAsLastSibling();
        if (leftPageBody != null)
            leftPageBody.transform.SetAsLastSibling();
        if (rightPageBody != null)
            rightPageBody.transform.SetAsLastSibling();
    }

    void ApplyCardImageLayout(float slotCenterY)
    {
        ApplySlotRect(_leftCardSlot, new Vector2(leftPageCenter.x, slotCenterY));
        ApplySlotRect(_rightCardSlot, new Vector2(rightPageCenter.x, slotCenterY));

        if (_leftCardFrame != null)
        {
            _leftCardFrame.sizeDelta = cardImageSize;
            _leftCardFrame.anchoredPosition = new Vector2(0f, FrameCenterYOffset);
        }

        if (_rightCardFrame != null)
        {
            _rightCardFrame.sizeDelta = cardImageSize;
            _rightCardFrame.anchoredPosition = new Vector2(0f, FrameCenterYOffset);
        }
    }

    void ApplySlotRect(RectTransform slot, Vector2 anchoredPosition)
    {
        if (slot == null)
            return;
        slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.anchoredPosition = anchoredPosition;
        slot.sizeDelta = BorderSize;
        ApplyBorderTexture(slot.GetComponent<RawImage>());
    }

    static void ApplyTextRect(TMP_Text textTmp, Vector2 pos, Vector2 size)
    {
        if (textTmp == null)
            return;
        var rt = textTmp.rectTransform;
        rt.anchoredPosition = pos;
        if (size.sqrMagnitude > 0f)
            rt.sizeDelta = size;
    }

    static void ApplyBodyRect(TMP_Text bodyTmp, Vector2 pos, Vector2 size)
    {
        if (bodyTmp == null)
            return;
        var rt = bodyTmp.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static string FormatRevealedCardBody(TarotCardData card)
    {
        string description = card.GetCardDescription();
        string theme = string.IsNullOrWhiteSpace(card.cardTheme) ? "Unknown" : card.cardTheme.Trim();
        string moral = FormatMoral(card.cardMoral);
        return $"<b>Theme:</b> {theme}\n<b>Morality:</b> {moral}\n\n{description}";
    }

    static string FormatMoral(TarotMoral moral)
    {
        return moral switch
        {
            TarotMoral.Good => "Good",
            TarotMoral.Bad => "Bad",
            _ => "Neutral"
        };
    }

    void SetSideImage(Image image, AspectRatioFitter fitter, RectTransform slot, Sprite sprite, bool visible, bool locked)
    {
        if (image == null || slot == null)
            return;

        bool show = visible && sprite != null;
        slot.gameObject.SetActive(show);

        if (!show)
            return;

        image.sprite = sprite;
        image.color = locked ? new Color(0.35f, 0.32f, 0.34f, 1f) : Color.white;
        image.enabled = true;

        if (fitter != null)
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        }

        slot.sizeDelta = BorderSize;
        ApplyBorderTexture(slot.GetComponent<RawImage>());
    }

    void SetCardImagesVisible(bool visible)
    {
        if (!visible)
        {
            if (_leftCardSlot != null)
                _leftCardSlot.gameObject.SetActive(false);
            if (_rightCardSlot != null)
                _rightCardSlot.gameObject.SetActive(false);
        }
    }

    static void ApplyTextPage(TMP_Text titleTmp, TMP_Text bodyTmp, List<PageData> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count || list[index] == null)
        {
            ClearTmp(titleTmp);
            ClearTmp(bodyTmp);
            return;
        }

        SetTmp(titleTmp, list[index].pageHeading);
        SetTmp(bodyTmp, list[index].pageWriting);
    }

    static void SetTmp(TMP_Text tmp, string value)
    {
        if (tmp == null)
            return;
        tmp.text = value ?? "";
    }

    static void ClearTmp(TMP_Text tmp)
    {
        if (tmp == null)
            return;
        tmp.text = "";
    }
}
