using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TarotCardPull tarotCardPull;

    //Buttons
    [SerializeField] Button drawCardsBtn;
    [SerializeField] Button readFortuneBtn;
    [SerializeField] Button tentBtn;
    [SerializeField] Button cardsBtn;
    [SerializeField] Button wisdonBookBtn;

    //Panels
    [SerializeField] GameObject cardUI;

    //Active Card Check
    private bool cardsPulled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        drawCardsBtn.onClick.AddListener(DrawCardsPressed);
        readFortuneBtn.onClick.AddListener(ReadFortunePressed);
        tentBtn.onClick.AddListener(TentPressed);
        cardsBtn.onClick.AddListener(CardsPressed);
        wisdonBookBtn.onClick.AddListener(WisdomBooksPressed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DrawCardsPressed()
    {
        if (!cardsPulled)
        {
            tarotCardPull.CardPull();
        }
        cardsPulled = true;
    }

    void ReadFortunePressed()
    {

    }

    void TentPressed()
    {

    }
    void CardsPressed()
    {

    }
    void WisdomBooksPressed()
    {

    }

    public void HideCardPanel()
    {
        cardUI.SetActive(false);
    }
    public void ShowCardPanel()
    {
        cardUI.SetActive(true);
    }


}
