using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reads card slot data from <see cref="TarotCardPull"/> only (no changes required on that script).
/// Resolves theme by matching the slot title back to <c>tarotDatabase.cards</c> for random-pull safety.
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
            if (pull.tarotDatabase != null && pull.tarotDatabase.cards != null)
            {
                for (int j = 0; j < pull.tarotDatabase.cards.Count; j++)
                {
                    TarotCardData dbCard = pull.tarotDatabase.cards[j];
                    if (dbCard == null || string.IsNullOrWhiteSpace(dbCard.cardName))
                        continue;
                    if (!string.Equals(dbCard.cardName.Trim(), title, System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    theme = dbCard.cardTheme ?? "";
                    break;
                }
            }

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
