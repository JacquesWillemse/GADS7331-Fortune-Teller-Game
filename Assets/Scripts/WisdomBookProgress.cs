using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks which tarot entries in the Book of Wisdom have been revealed this run.
/// A card unlocks when it appears in the spread and the player names its theme in a reading.
/// </summary>
public class WisdomBookProgress : MonoBehaviour
{
    [SerializeField] private TarotCards tarotDatabase;

    readonly HashSet<int> _revealedCardIndices = new HashSet<int>();

    public UnityEvent<int> onCardRevealed;

    public void ResetProgress()
    {
        _revealedCardIndices.Clear();
    }

    /// <summary>Reveal the first N pasteboard pages at run start (lineage difficulty).</summary>
    public void ApplyStartingUnlocks(int count)
    {
        if (tarotDatabase?.cards == null || count <= 0)
            return;

        int n = Mathf.Min(count, tarotDatabase.cards.Count);
        for (int i = 0; i < n; i++)
        {
            if (_revealedCardIndices.Contains(i))
                continue;
            _revealedCardIndices.Add(i);
            onCardRevealed?.Invoke(i);
        }
    }

    public bool IsRevealed(int cardIndex)
    {
        return cardIndex >= 0 && _revealedCardIndices.Contains(cardIndex);
    }

    /// <summary>After Read Fortune — unlock any drawn card whose theme was identified in the text.</summary>
    public void TryUnlockFromReading(IReadOnlyList<TarotCardData> spread, string playerReading)
    {
        if (tarotDatabase?.cards == null || spread == null || string.IsNullOrWhiteSpace(playerReading))
            return;

        foreach (TarotCardData drawn in spread)
        {
            if (drawn == null || !FortuneDuelRubric.PlayerIdentifiedCardTheme(playerReading, drawn))
                continue;

            int index = IndexOfCard(drawn);
            if (index < 0 || _revealedCardIndices.Contains(index))
                continue;

            _revealedCardIndices.Add(index);
            onCardRevealed?.Invoke(index);
        }
    }

    int IndexOfCard(TarotCardData card)
    {
        for (int i = 0; i < tarotDatabase.cards.Count; i++)
        {
            TarotCardData entry = tarotDatabase.cards[i];
            if (entry == null)
                continue;
            if (ReferenceEquals(entry, card))
                return i;
            if (string.Equals(entry.cardName?.Trim(), card.cardName?.Trim(), StringComparison.Ordinal))
                return i;
        }
        return -1;
    }
}
