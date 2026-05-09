using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TarotCardPull : MonoBehaviour
{
    public TarotCards tarotDatabase;

    //CardData
    public TMP_Text[] cardDescriptions;
    public Image[] cardImages;

    private int cardDrawsAmount = 3;
    private int cardCount = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cardCount = tarotDatabase.cards.Count;
        CardPull();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CardPull()
    {
        TarotCardData card = tarotDatabase.cards[0];
        for (int i = 0; i < cardDrawsAmount; i++)
        {
            card = tarotDatabase.cards[i];
            cardDescriptions[i].text = card.cardName;
            cardImages[i] = card.tarotCardImage;
            Debug.Log(card.cardTheme);
        }
    }
}
