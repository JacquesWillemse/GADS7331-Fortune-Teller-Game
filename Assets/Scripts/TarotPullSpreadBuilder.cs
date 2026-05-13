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

            if (string.IsNullOrWhiteSpace(theme))
                theme = InferCarnivalThemeFromTitle(title);

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

    /// <summary>
    /// When the database has no row matching the pulled title, infer a carnival theme so LLM prompts and
    /// scoring see stable lane names instead of empty tags (which caused models to invent wrong lanes).
    /// </summary>
    static string InferCarnivalThemeFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "";
        string t = title.ToLowerInvariant();

        if (t.Contains("greed"))
            return "Greed";
        if (t.Contains("vanity"))
            return "Vanity";
        if (t.Contains("chaos"))
            return "Chaos";
        if (t.Contains("power"))
            return "Power";

        if (t.Contains("mirror") || t.Contains("mohawk") || t.Contains("waxing") || t.Contains("haircut") || (t.Contains("ego") && t.Contains("pride")))
            return "Vanity";

        if (t.Contains("wrestling") || (t.Contains("mayhem") && t.Contains("crowd")))
            return "Chaos";

        if (t.Contains("shoot") || t.Contains("shot") || t.Contains("throne") || t.Contains("crown") || t.Contains("remote") ||
            t.Contains("dominion") || (t.Contains("command") && t.Contains("weight")))
            return "Power";

        if (t.Contains("meal") || t.Contains("meals") || t.Contains("cheese") || t.Contains("succulent") || t.Contains("fry") ||
            t.Contains("feast") || t.Contains("banquet") || t.Contains("glutton") || t.Contains("chinese") ||
            (t.Contains("rat") && (t.Contains("cheese") || t.Contains("fry"))))
            return "Greed";

        return "";
    }
}
