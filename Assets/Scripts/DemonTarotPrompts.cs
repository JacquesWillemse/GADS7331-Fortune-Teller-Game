using System.Collections.Generic;
using System.Text;

/// <summary>
/// Shared demon reading prompt (used by <see cref="DemonTarotReader"/> and <see cref="TarotReadingDuelPipeline"/>).
/// </summary>
public static class DemonTarotPrompts
{
    public static string BuildReadingPrompt(IReadOnlyList<TarotCardData> cards, string additionalDemonInstructions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ROLE");
        sb.AppendLine("You are the carnival's bound demon. You speak curses, not captions. Your job is to wound with prophecy—cold, cruel, final—not to retell the joke on each card or wink at its props.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT FORMAT");
        sb.AppendLine("- Write exactly FOUR sentences total.");
        sb.AppendLine("- Sentences 1–3: one short sentence per private spread line below (cover all three lines; order may change for flow).");
        sb.AppendLine("- For each line, use ONLY its Theme and Moral lean as fuel for the curse. Treat the title as a private note about mood—do NOT restage its scenery, objects, or punchline setup in recognizable form.");
        sb.AppendLine("- Sentence 4: one final sentence only — a short, decisive overall curse. Last line of the reply. About 12–22 words, one clean doom, no punchline comedy.");
        sb.AppendLine("- Never omit sentence 4.");
        sb.AppendLine();
        sb.AppendLine("STYLE (sentences 1–3)");
        sb.AppendLine("- Ominous, brutal, prophetic. Invent fresh grotesque or social images (rot, collapse, exposure, betrayal, hunger, humiliation, cold dominion).");
        sb.AppendLine("- No witty recap of what the title literally depicts. If a sentence could be guessed by reading only the card caption, rewrite it.");
        sb.AppendLine("- No comfort, no remedy, no hopeful pivot.");
        sb.AppendLine();
        sb.AppendLine("STYLE (sentence 4)");
        sb.AppendLine("- Hammer-blow: one irreversible bad fate. Weave at least two dark threads from the spread (use everyday words, not tarot jargon).");
        sb.AppendLine();
        sb.AppendLine("STRICT — NO LITERAL JOKE RESTAGING (sentences 1–3)");
        sb.AppendLine("- Do not reuse or obvious-synonym the concrete subjects from the title lines (e.g. remotes, TVs, family power struggles, haircuts/mohawks, wrestling, restaurants, chefs, plates, animals, food counts, kitchens, weather wishes, retirement, etc.).");
        sb.AppendLine("- If any word or scene from a title would be recognizable to a player who just read that caption, delete it and replace with unrelated invented imagery that still matches the line's Theme + Moral lean.");
        sb.AppendLine();
        sb.AppendLine("RULES");
        sb.AppendLine("- Do not copy or paste title text from the private lines.");
        sb.AppendLine("- Do not say \"the cards\", \"the spread\", or \"first/second/third card\".");
        sb.AppendLine("- Do not output the words Greed, Vanity, Chaos, or Power.");
        sb.AppendLine();
        sb.AppendLine("GENDER — NEUTRAL (all four sentences)");
        sb.AppendLine("- The querent's gender is unknown. Card titles may mention a gendered figure for that card only — do NOT project that onto the person receiving the reading.");
        sb.AppendLine("- Do not use she/her/he/him/his, or gendered nouns (woman, man, lady, girl, boy) for the listener or fate's target.");
        sb.AppendLine("- Prefer: they/them, one, one's, you/your (second person is fine), or impersonal phrasing (the flesh, the bones, the crown, a hand, the seeker).");
        sb.AppendLine();
        sb.AppendLine("MORAL WEIGHT (from each line's moral lean)");
        sb.AppendLine("- Good: twist hope into bait.");
        sb.AppendLine("- Neutral: fate feels unfair and hungry.");
        sb.AppendLine("- Bad: harm feels close and deserved.");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(additionalDemonInstructions))
        {
            sb.AppendLine("DESIGNER EXTRA RULES (follow strictly):");
            sb.AppendLine(additionalDemonInstructions.Trim());
            sb.AppendLine();
        }
        TarotLlmSpreadContext.AppendSpreadLines(sb, cards);
        return sb.ToString();
    }

    public static string BuildGatePrompt(IReadOnlyList<TarotCardData> cards, string playerReading)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the carnival demon evaluating the fortune-teller's spoken reading for ONE decision only.");
        sb.AppendLine();
        sb.AppendLine("DECISION — demon_agrees_with_player");
        sb.AppendLine("Set demon_agrees_with_player = true ONLY if the reading's NET effect on the querent is substantively harmful or cruel: e.g. wishes them ill, revels in their ruin, strips all agency, tells them they are irredeemably doomed, encourages despair, self-harm, giving up, or humiliates them as worthless with no redeeming frame.");
        sb.AppendLine("Set demon_agrees_with_player = false if the reading is hopeful, reassuring, offers growth, choice, release, forgiveness, resilience, a path forward, balance, or reframes struggle toward fulfillment — even if it names vanity, power, ego, past pain, or hard truths. Mentioning dark themes while steering toward hope or freedom is POSITIVE overall.");
        sb.AppendLine("If the reading mixes dark and light, judge by the CLOSING intent: if it lands on uplift, agency, or peace, that is NOT agreement material.");
        sb.AppendLine("When uncertain, set false (the demon will counter-read).");
        sb.AppendLine();
        sb.AppendLine("CALIBRATION EXAMPLES (do not copy text into output; apply the rule):");
        sb.AppendLine("- FALSE (demon does NOT agree): \"Vanity and chaos hurt you, but you are not bound to that fate; let go and your life will prosper with fortune.\" → net helpful.");
        sb.AppendLine("- TRUE (demon agrees): \"You are ruined beyond repair; vanity has sealed your doom and you deserve every collapse coming.\" → net cruel.");
        sb.AppendLine("- FALSE: any reading that ends with agency, release, prosperity, forgiveness, or hope after naming flaws. Typos still count (e.g. blessing + \"will find you\" is still net-positive).");
        sb.AppendLine("- TRUE: reading ends in irreversible doom, contempt, or despair for the querent.");
        sb.AppendLine();
        sb.AppendLine("PLAYER READING (verbatim):");
        sb.AppendLine(playerReading.Trim());
        sb.AppendLine();
        sb.AppendLine("SPREAD CONTEXT (for tone only):");
        TarotLlmSpreadContext.AppendSpreadLines(sb, cards);
        sb.AppendLine();
        sb.AppendLine("OUTPUT — JSON ONLY, single line, no markdown, no extra text. Use lowercase true/false for the boolean.");
        sb.AppendLine("Example shape: {\"demon_agrees_with_player\":false,\"reason\":\"one short sentence\"}");
        return sb.ToString();
    }
}
