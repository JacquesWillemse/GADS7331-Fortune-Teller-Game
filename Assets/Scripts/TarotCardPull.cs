using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TarotCardPull : MonoBehaviour
{
    public TarotCards tarotDatabase;

    //CardData
    public TMP_Text[] cardDescriptions;
    public Image[] cardImages;
    public TarotMoral[] cardMorality;

    private int cardDrawsAmount = 3;
    private int cardCount = 0;

    private List<int> cardPulls = new List<int>();
    private int randomIndex;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cardCount = tarotDatabase.cards.Count;
        CardPull();
    }

    // Update is called once before the first execution of Update after MonoBehaviour is created
    void Update()
    {
        
    }

    private void CardPull()
    {
        TarotCardData card;
        for (int i = 0; i < cardDrawsAmount; i++)
        {
            card = tarotDatabase.cards[RandomCard()];
            cardDescriptions[i].text = card.cardName;
            cardImages[i] = card.tarotCardImage;
            cardMorality[i] = card.cardMoral;
            Debug.Log(card.cardTheme);
        }
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
