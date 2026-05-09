using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reads card slot data from <see cref="TarotCardPull"/> only (no changes required on that script).
/// Theme is taken from <c>tarotDatabase.cards[i]</c> when indices align with the pull order.
/// </summary>
public static class TarotPullSpreadBuilder
{
    public static bool TryBuildSpreadForLlm(List<TarotCardData> cards, TarotCardPull pull)
    {
        cards.Clear();
        if (pull == null || pull.cardDescriptions == null || pull.cardMorality == null)
            return false;

        int n = Mathf.Min(pull.cardDescriptions.Length, pull.cardMorality.Length);
        if (pull.cardImages != null && pull.cardImages.Length < n)
            n = pull.cardImages.Length;

        for (int i = 0; i < n; i++)
        {
            if (pull.cardDescriptions[i] == null)
                return false;

            string title = pull.cardDescriptions[i].text?.Trim();
            if (string.IsNullOrEmpty(title))
                return false;

            string theme = "";
            if (pull.tarotDatabase != null && pull.tarotDatabase.cards != null && i < pull.tarotDatabase.cards.Count)
                theme = pull.tarotDatabase.cards[i].cardTheme ?? "";

            cards.Add(new TarotCardData
            {
                cardName = title,
                cardTheme = theme,
                cardMoral = pull.cardMorality[i],
                tarotCardImage = null
            });
        }

        return cards.Count > 0;
    }
}
