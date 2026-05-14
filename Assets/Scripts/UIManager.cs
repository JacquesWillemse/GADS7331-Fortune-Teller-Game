using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private FortuneFlowController fortuneFlow;
    [SerializeField] private GameManager gameManager;

    [SerializeField] private Button drawCardsBtn;
    [SerializeField] private Button readFortuneBtn;
    [SerializeField] private Button tentBtn;
    [SerializeField] private Button cardsBtn;
    [SerializeField] private Button wisdonBookBtn;
    [SerializeField] private Button spiritBtn;
    [SerializeField] private Button judgeBtn;
    [Tooltip("Runs scoring and fills judge text (e.g. spirit / judge screen).")]
    [SerializeField] private Button renderJudgementBtn;
    [Tooltip("Applies energy & customers and clears the tent for the next customer.")]
    [SerializeField] private Button acceptJudgementBtn;

    [SerializeField] private GameObject cardUI;

    void Start()
    {
        if (drawCardsBtn != null)
            drawCardsBtn.onClick.AddListener(DrawCardsPressed);
        if (readFortuneBtn != null)
            readFortuneBtn.onClick.AddListener(ReadFortunePressed);
        if (tentBtn != null)
            tentBtn.onClick.AddListener(TentPressed);
        if (cardsBtn != null)
            cardsBtn.onClick.AddListener(CardsPressed);
        if (wisdonBookBtn != null)
            wisdonBookBtn.onClick.AddListener(WisdomBooksPressed);
        if (spiritBtn != null)
            spiritBtn.onClick.AddListener(SpiritPressed);
        if (judgeBtn != null)
            judgeBtn.onClick.AddListener(JudgePressed);
        if (renderJudgementBtn != null)
            renderJudgementBtn.onClick.AddListener(RenderJudgementPressed);
        if (acceptJudgementBtn != null)
            acceptJudgementBtn.onClick.AddListener(AcceptJudgementPressed);
    }

    void DrawCardsPressed()
    {
        fortuneFlow?.OnDrawCards();
    }

    void ReadFortunePressed()
    {
        fortuneFlow?.OnReadFortune();
    }

    void TentPressed()
    {
        cameraManager?.ActivateTentCamera();
    }

    void CardsPressed()
    {
        fortuneFlow?.OnCardsViewOpened();
        cameraManager?.ActivateCardCamera();
    }

    void WisdomBooksPressed()
    {
        cameraManager?.ActivateBookCamera();
    }

    void SpiritPressed()
    {
        fortuneFlow?.OnCardsViewOpened();
        cameraManager?.ActivateSpiritCamera();
    }

    void JudgePressed()
    {
        cameraManager?.ActivateJudgeCamera();
    }

    void RenderJudgementPressed()
    {
        fortuneFlow?.RenderVerdict();
    }

    void AcceptJudgementPressed()
    {
        gameManager?.AcceptJudgement();
    }

    public void HideCardPanel()
    {
        if (cardUI != null)
            cardUI.SetActive(false);
    }

    public void ShowCardPanel()
    {
        if (cardUI != null)
            cardUI.SetActive(true);
    }
}
