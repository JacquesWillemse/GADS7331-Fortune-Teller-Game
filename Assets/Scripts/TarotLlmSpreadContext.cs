using System.Collections.Generic;
using System.Text;

/// <summary>
/// Shared formatting of card rows for Ollama prompts (smoke test, demon, judge later).
/// </summary>
public static class TarotLlmSpreadContext
{
    public static void AppendSpreadLines(StringBuilder sb, IReadOnlyList<TarotCardData> cards)
    {
        sb.AppendLine("Private spread context (never repeat verbatim):");
        for (int i = 0; i < cards.Count; i++)
        {
            TarotCardData c = cards[i];
            sb.Append(i + 1).Append(". ").Append(c.cardName?.Trim() ?? "?");
            sb.Append(" | Theme: ").Append(string.IsNullOrEmpty(c.cardTheme) ? "?" : c.cardTheme);
            sb.Append(" | Moral lean: ").Append(c.cardMoral);
            sb.AppendLine();
        }
    }
}
