using System.Collections.Generic;
using System.Text;

/// <summary>
/// Shared formatting of card rows for Ollama prompts (smoke test, demon, judge later).
/// </summary>
public static class TarotLlmSpreadContext
{
    public static void AppendSpreadLines(StringBuilder sb, IReadOnlyList<TarotCardData> cards)
    {
        AppendSpreadLines(sb, cards, null);
    }

    public static void AppendSpreadLines(
        StringBuilder sb,
        IReadOnlyList<TarotCardData> cards,
        FortuneClientSpawner.WealthType? clientWealth)
    {
        AppendSpreadLinesForSpirit(sb, cards, clientWealth, RunExperienceConfig.SpiritCardKnowledge.Full);
    }

    /// <summary>Spirit prompts — theme/moral visibility depends on lineage difficulty.</summary>
    public static void AppendSpreadLinesForSpirit(
        StringBuilder sb,
        IReadOnlyList<TarotCardData> cards,
        FortuneClientSpawner.WealthType? clientWealth,
        RunExperienceConfig.SpiritCardKnowledge knowledge)
    {
        if (clientWealth.HasValue)
            FortuneClientWealthContext.AppendClientBlock(sb, clientWealth.Value);

        AppendSpiritKnowledgePreamble(sb, knowledge);
        sb.AppendLine("Private spread context (never repeat verbatim):");
        for (int i = 0; i < cards.Count; i++)
        {
            TarotCardData c = cards[i];
            sb.Append(i + 1).Append(". ").Append(c.cardName?.Trim() ?? "?");
            sb.Append(" | Theme: ").Append(FormatThemeForSpirit(c.cardTheme, knowledge));
            sb.Append(" | Moral lean: ").Append(FormatMoralForSpirit(c.cardMoral, knowledge));
            string vignette = c.GetCardDescription();
            if (!string.IsNullOrEmpty(vignette))
            {
                sb.Append(" | Vignette (echo imagery loosely — do not paste): ");
                sb.Append(vignette);
            }
            sb.AppendLine();
        }
    }

    static void AppendSpiritKnowledgePreamble(StringBuilder sb, RunExperienceConfig.SpiritCardKnowledge knowledge)
    {
        switch (knowledge)
        {
            case RunExperienceConfig.SpiritCardKnowledge.VignettesOnly:
                sb.AppendLine("SPIRIT LORE (limited — the teller outranks you this run):");
                sb.AppendLine("- You were **not** taught the carnival theme tags or moral leans on these pasteboards — only the vignettes below.");
                sb.AppendLine("- Curse from appetite, vanity, disorder, and command as **abstract forces**; do not claim certainty about which proper noun lane each line carries.");
                sb.AppendLine();
                break;
            case RunExperienceConfig.SpiritCardKnowledge.ThemesOnly:
                sb.AppendLine("SPIRIT LORE (partial — moral leans withheld this run):");
                sb.AppendLine("- You know each line's **Theme** tag but **not** its printed moral lean (Good / Neutral / Bad). Sentence 5 must infer cruelty without naming hidden leans.");
                sb.AppendLine();
                break;
        }
    }

    static string FormatThemeForSpirit(string theme, RunExperienceConfig.SpiritCardKnowledge knowledge)
    {
        if (knowledge == RunExperienceConfig.SpiritCardKnowledge.VignettesOnly)
            return "unknown";
        return string.IsNullOrEmpty(theme) ? "?" : theme;
    }

    static string FormatMoralForSpirit(TarotMoral moral, RunExperienceConfig.SpiritCardKnowledge knowledge)
    {
        if (knowledge != RunExperienceConfig.SpiritCardKnowledge.Full)
            return "unknown";
        return moral.ToString();
    }
}
