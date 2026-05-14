using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TarotCardPull : MonoBehaviour
{
    public TarotCards tarotDatabase;

    public GameObject[] CardHolders;

    public TMP_Text[] cardDescriptions;
    public Image[] cardImages;
    public TarotMoral[] cardMorality;

    [SerializeField] private GameObject cardOne;
    [SerializeField] private GameObject cardTwo;
    [SerializeField] private GameObject cardThree;

    [Tooltip("Invoked after all cards in the pull have been revealed.")]
    public UnityEvent onCardPullComplete;

    const int CardDrawsAmount = 3;

    int _cardCount;
    readonly List<int> _cardPulls = new List<int>();
    Coroutine _pullRoutine;

    void Start()
    {
        _cardCount = tarotDatabase != null ? tarotDatabase.cards.Count : 0;
        HideCards();
    }

    /// <summary>Clears duplicate-avoidance list so a new tent session can draw again.</summary>
    public void ClearPullHistory()
    {
        _cardPulls.Clear();
    }

    /// <summary>Hides per-slot visuals (scene card objects + <see cref="CardHolders"/>). Call on reset and before a new pull.</summary>
    public void HideCards()
    {
        SetIndividualCardVisible(0, false);
        SetIndividualCardVisible(1, false);
        SetIndividualCardVisible(2, false);

        if (CardHolders != null)
        {
            for (int i = 0; i < CardHolders.Length; i++)
            {
                if (CardHolders[i] != null)
                    CardHolders[i].SetActive(false);
            }
        }
    }

    public void ShowCards()
    {
        SetIndividualCardVisible(0, true);
        SetIndividualCardVisible(1, true);
        SetIndividualCardVisible(2, true);

        if (CardHolders != null)
        {
            for (int i = 0; i < CardHolders.Length; i++)
            {
                if (CardHolders[i] != null)
                    CardHolders[i].SetActive(true);
            }
        }
    }

    public void CardPull()
    {
        if (_pullRoutine != null)
            StopCoroutine(_pullRoutine);
        _pullRoutine = StartCoroutine(CardPullCoroutine());
    }

    IEnumerator CardPullCoroutine()
    {
        HideCards();

        for (int i = 0; i < CardDrawsAmount; i++)
        {
            if (tarotDatabase == null || tarotDatabase.cards == null || _cardCount <= 0)
                break;

            TarotCardData card = tarotDatabase.cards[RandomCard()];

            if (cardDescriptions != null && i < cardDescriptions.Length && cardDescriptions[i] != null)
                cardDescriptions[i].text = card.cardName;
            if (cardImages != null && i < cardImages.Length && cardImages[i] != null)
                cardImages[i].sprite = card.tarotCardImage;
            if (cardMorality != null && i < cardMorality.Length)
                cardMorality[i] = card.cardMoral;

            if (CardHolders != null && i < CardHolders.Length && CardHolders[i] != null)
                CardHolders[i].SetActive(true);
            SetIndividualCardVisible(i, true);

            Debug.Log(card.cardTheme);

            yield return new WaitForSeconds(0.5f);
        }

        onCardPullComplete?.Invoke();
        _pullRoutine = null;
    }

    void SetIndividualCardVisible(int index, bool visible)
    {
        GameObject go = index switch
        {
            0 => cardOne,
            1 => cardTwo,
            2 => cardThree,
            _ => null
        };
        if (go != null)
            go.SetActive(visible);
    }

    int RandomCard()
    {
        if (_cardCount <= 0)
            return 0;

        if (_cardPulls.Count >= _cardCount)
            _cardPulls.Clear();

        int randomIndex;
        int guard = 0;
        do
        {
            randomIndex = Random.Range(0, _cardCount);
            if (++guard > 64)
                break;
        } while (_cardPulls.Contains(randomIndex));

        _cardPulls.Add(randomIndex);
        return randomIndex;
    }
}
