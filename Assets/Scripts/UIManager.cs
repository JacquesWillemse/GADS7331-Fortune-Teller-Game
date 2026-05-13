using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private FortuneFlowController fortuneFlow;

    [SerializeField] private Button drawCardsBtn;
    [SerializeField] private Button readFortuneBtn;
    [SerializeField] private Button tentBtn;
    [SerializeField] private Button cardsBtn;
    [SerializeField] private Button wisdonBookBtn;
    [SerializeField] private Button spiritBtn;
    [SerializeField] private Button judgeBtn;

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
        // Start the spirit request while this UI (and FortuneFlow) may still be enabled; camera switches next.
        fortuneFlow?.OnCardsViewOpened();
        cameraManager?.ActivateCardCamera();
    }

    void WisdomBooksPressed()
    {
        cameraManager?.ActivateBookCamera();
    }

    void SpiritPressed()
    {
        // If the player opens Spirit without using the Cards tab, still request the reading once cards are drawn.
        fortuneFlow?.OnCardsViewOpened();
        cameraManager?.ActivateSpiritCamera();
    }

    void JudgePressed()
    {
        cameraManager?.ActivateJudgeCamera();
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
