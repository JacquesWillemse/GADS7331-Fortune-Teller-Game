using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TarotCardPull : MonoBehaviour
{
    public TarotCards tarotDatabase;

    //CardData
    public GameObject[] CardHolders;

    public TMP_Text[] cardDescriptions;
    public Image[] cardImages;
    public TarotMoral[] cardMorality;

    [Tooltip("Invoked after all cards in the pull have been revealed.")]
    public UnityEvent onCardPullComplete;

    private int cardDrawsAmount = 3;
    private int cardCount = 0;

    private List<int> cardPulls = new List<int>();
    private int randomIndex;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cardCount = tarotDatabase.cards.Count;
        //CardPull();
    }

    // Update is called once before the first execution of Update after MonoBehaviour is created
    void Update()
    {
        
    }
    public void CardPull()
    {
        StartCoroutine(CardPullCoroutine());
    }

    private IEnumerator CardPullCoroutine()
    {
        TarotCardData card;

        for (int i = 0; i < cardDrawsAmount; i++)
        {
            card = tarotDatabase.cards[RandomCard()];

            cardDescriptions[i].text = card.cardName;
            cardImages[i] = card.tarotCardImage;
            cardMorality[i] = card.cardMoral;

            CardHolders[i].SetActive(true);

            Debug.Log(card.cardTheme);

            yield return new WaitForSeconds(0.5f);
        }

        onCardPullComplete?.Invoke();
    }

    private int RandomCard()
    {
        do
        {
            randomIndex = UnityEngine.Random.Range(0, cardCount);
        }
        while (cardPulls.Contains(randomIndex));
        cardPulls.Add(randomIndex);
        return randomIndex;
    }
}
