using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Prose-only judge prompt: <see cref="FortuneDuelRubric"/> decides the outcome; the LLM narrates it in tent voice.
/// </summary>
public static class JudgeVerdictProsePrompts
{
    public static string BuildProsePrompt(
        IReadOnlyList<TarotCardData> spread,
        FortuneDuelScoreBreakdown duel,
        bool playerWon,
        bool guaranteedWin,
        float magicalEnergy0to100,
        string officialOneLineSummary,
        string playerReading,
        string spiritReading,
        FortuneClientSpawner.WealthType clientWealth)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ROLE");
        sb.AppendLine("You speak as **the tent** — the carnival fortune booth itself — delivering the final ruling to **the customer** who waited for this duel.");
        sb.AppendLine("The numeric tally is already settled. Your job is a short, spoken verdict in the tent's voice (impersonal or \"the tent\" — never a separate arbiter character).");
        sb.AppendLine();
        sb.AppendLine("CAST — you may name ONLY these four (exact phrases below):");
        sb.AppendLine("- **the tent**");
        sb.AppendLine("- **the fortune teller** (the human reader; never \"teller\" alone, never \"player\")");
        sb.AppendLine("- **the spirit** (the demon's curse; never \"demon\" unless quoting is unavoidable — prefer **the spirit**)");
        sb.AppendLine("- **the customer** (the person receiving the reading; never querent, seeker, listener, audience, crowd, or \"you\" addressing a camera)");
        sb.AppendLine();
        sb.AppendLine("FORBIDDEN IN PROSE");
        sb.AppendLine("- First person as a judge: no I, me, my, we proclaim, I am the arbiter.");
        sb.AppendLine("- Extra cast: no crowd, carnival, arbiter, booth judge, ancient voice, epic duel, onlookers, fairground, ringmaster.");
        sb.AppendLine("- Score dump: do not recite point totals, margins, tallies, \"44 points\", \"+24\", sliders, or rubric labels.");
        sb.AppendLine("- Theme words **Greed**, **Vanity**, **Chaos**, **Power** may appear when weaving the draw (required below) — but do not lecture about mirrors, hunger, etc. at length; one light touch per theme is enough.");
        sb.AppendLine("- You may allude lightly to whether **the customer** is wealthy or poor when explaining who read the booth better — do not name 3D props.");
        sb.AppendLine("- Markdown, headings, JSON, preamble.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT");
        sb.AppendLine("- **2–4 sentences**, one paragraph.");
        sb.AppendLine("- Explain *why* in story language tied to how the two readings sat against the draw — not as a spreadsheet.");
        sb.AppendLine("- Address or acknowledge **the customer** at least once.");
        sb.AppendLine("- **FINAL SENTENCE (mandatory):** End with one short, unmistakable declaration of who won the duel. Use the exact phrase **the fortune teller** or **the spirit** (whichever matches SETTLED FACTS).");
        if (playerWon)
            sb.AppendLine("  Required closing pattern (adapt wording slightly if needed, but the last sentence must clearly say the fortune teller won): e.g. \"…and so the tent awards this duel to the fortune teller.\"");
        else
            sb.AppendLine("  Required closing pattern (adapt wording slightly if needed, but the last sentence must clearly say the spirit won): e.g. \"…and so the tent awards this duel to the spirit.\"");
        sb.AppendLine("- Do not end on ambiguity, a tie question, or \"only time will tell.\"");
        sb.AppendLine();
        AppendRequiredKeywords(sb, spread, playerWon, guaranteedWin, magicalEnergy0to100, clientWealth);
        sb.AppendLine();
        AppendSettledFacts(sb, duel, playerWon, guaranteedWin, magicalEnergy0to100, officialOneLineSummary, clientWealth);
        sb.AppendLine();
        sb.AppendLine("CONTEXT (for meaning only — do not quote either reading at length):");
        sb.AppendLine("FORTUNE TELLER READING:");
        sb.AppendLine(TrimForPrompt(playerReading, 900));
        sb.AppendLine();
        sb.AppendLine("SPIRIT READING:");
        sb.AppendLine(TrimForPrompt(spiritReading, 900));
        sb.AppendLine();
        TarotLlmSpreadContext.AppendSpreadLines(sb, spread, clientWealth);
        return sb.ToString();
    }

    static void AppendRequiredKeywords(
        StringBuilder sb,
        IReadOnlyList<TarotCardData> spread,
        bool playerWon,
        bool guaranteedWin,
        float magicalEnergy0to100,
        FortuneClientSpawner.WealthType clientWealth)
    {
        var words = new List<string> { "the tent", "the fortune teller", "the spirit", "the customer" };
        CollectThemeWords(spread, words);

        if (playerWon)
            words.Add("the fortune teller");
        else
            words.Add("the spirit");

        words.Add("favor");

        if (guaranteedWin || magicalEnergy0to100 >= 99.5f)
            words.Add("magical energy");
        else if (magicalEnergy0to100 > 0f)
            words.Add("magical energy");

        words.Add(FortuneClientWealthContext.LabelFor(clientWealth));

        sb.AppendLine("REQUIRED PHRASES (each must appear at least once, exact wording):");
        var seen = new HashSet<string>();
        for (int i = 0; i < words.Count; i++)
        {
            string w = words[i];
            if (string.IsNullOrEmpty(w) || !seen.Add(w))
                continue;
            sb.Append("- ").AppendLine(w);
        }
    }

    static void CollectThemeWords(IReadOnlyList<TarotCardData> spread, List<string> words)
    {
        if (spread == null)
            return;
        var seen = new HashSet<string>();
        int n = spread.Count > 3 ? 3 : spread.Count;
        for (int i = 0; i < n; i++)
        {
            string theme = DemonTarotPrompts.ThemeProperNounForCard(spread[i]);
            if (seen.Add(theme))
                words.Add(theme);
        }
    }

    static void AppendSettledFacts(
        StringBuilder sb,
        FortuneDuelScoreBreakdown duel,
        bool playerWon,
        bool guaranteedWin,
        float magicalEnergy0to100,
        string officialOneLineSummary,
        FortuneClientSpawner.WealthType clientWealth)
    {
        sb.AppendLine("SETTLED FACTS (author-only — match the winner; do NOT repeat these numbers in your prose):");
        sb.Append("- Winner: ").AppendLine(playerWon ? "the fortune teller" : "the spirit");
        sb.Append("- Customer wealth: ").AppendLine(FortuneClientWealthContext.LabelFor(clientWealth));
        if (guaranteedWin)
            sb.AppendLine("- Outcome: full magical energy — fortune teller wins by decree.");
        sb.Append("- Fortune teller favor total: ").Append(duel.PlayerTotal).AppendLine(" (internal).");
        sb.Append("- Spirit favor total: ").Append(duel.DemonTotal).AppendLine(" (internal).");
        int margin = duel.PlayerTotal - duel.DemonTotal;
        sb.Append("- Margin: ").Append(margin).AppendLine(" (internal; do not say aloud).");
        sb.Append("- Magical energy committed: ").Append(Mathf.RoundToInt(Mathf.Clamp(magicalEnergy0to100, 0f, 100f))).AppendLine(".");
        if (duel.PlayerMagicPower > 0)
            sb.AppendLine("- Magical favor helped the fortune teller (allude in story if energy was spent; do not cite +points).");
        if (duel.PlayerSpreadMoralJudgeBias > 0)
            sb.AppendLine("- The draw leaned Good — tailwind for the fortune teller.");
        if (duel.DemonSpreadMoralJudgeBias > 0)
            sb.AppendLine("- The draw leaned Bad — tailwind for the spirit.");
        if (duel.PlayerNeutralSpreadBonus > 0)
            sb.AppendLine("- All-Neutral draw with teller hope-language — small booth steadiness for the fortune teller.");
        if (duel.PlayerThemeIdentification > duel.DemonThemeIdentification)
            sb.AppendLine("- Theme echo favored the fortune teller's wording.");
        else if (duel.DemonThemeIdentification > duel.PlayerThemeIdentification)
            sb.AppendLine("- Theme echo favored the spirit's curse.");
        if (duel.PlayerDescriptionEcho > duel.DemonDescriptionEcho)
            sb.AppendLine("- Vignette echo (card imagery in words) favored the fortune teller.");
        else if (duel.DemonDescriptionEcho > duel.PlayerDescriptionEcho)
            sb.AppendLine("- Vignette echo favored the spirit's curse.");
        if (duel.PlayerWealthFit > duel.DemonWealthFit)
            sb.AppendLine("- Reading fit the customer's wealth better on the fortune teller's side.");
        else if (duel.DemonWealthFit > duel.PlayerWealthFit)
            sb.AppendLine("- Reading fit the customer's wealth better on the spirit's side.");
        if (duel.PlayerAlignment > duel.DemonAlignment)
            sb.AppendLine("- Hope-tone favored the fortune teller.");
        else if (duel.DemonAlignment > duel.PlayerAlignment)
            sb.AppendLine("- Harsh-tone favored the spirit.");
        if (!string.IsNullOrWhiteSpace(officialOneLineSummary))
            sb.Append("- Meaning to honor: ").AppendLine(officialOneLineSummary.Trim());
    }

    static string TrimForPrompt(string text, int maxChars)
    {
        string t = text?.Trim() ?? "";
        if (t.Length <= maxChars)
            return string.IsNullOrEmpty(t) ? "(empty)" : t;
        return t.Substring(0, maxChars) + "…";
    }
}
