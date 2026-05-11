using System.Collections.Generic;
using System.Text;

public static class TarotJudgePrompts
{
    public static string BuildJudgePrompt(
        IReadOnlyList<TarotCardData> cards,
        string playerReading,
        string demonReading,
        int playerEnergyBonus0to10)
    {
        int energy = UnityEngine.Mathf.Clamp(playerEnergyBonus0to10, 0, TarotJudgeRubric.MaxEnergyBonus);

        var sb = new StringBuilder();
        sb.AppendLine("You are an ancient impartial arbiter at a carnival tent. You judge a DUEL between two opposed voices using the numeric rubric below. The game will overwrite player energy_bonus with the value given at the end; still output all score fields.");
        sb.AppendLine();
        sb.AppendLine("FIXED ROLES — do not swap or merge these:");
        sb.AppendLine("- FORTUNE TELLER (labeled PLAYER READING below): must help the querent. Hope, agency, moral balance, earned peace, or tough love that still aims at the querent's wellbeing counts here. This role is NEVER predatory and NEVER the soul-taking voice.");
        sb.AppendLine("- DEMON (labeled DEMON READING below): must harm, unsettle, corrupt, or doom the querent. Dread, shame, appetite for ruin, or predatory manipulation counts here. This role is NEVER protective, NEVER wellness advice, and NEVER \"helpful warning\" framed as care for the querent.");
        sb.AppendLine("- If you call the demon \"protective\", \"clarity for safety\", \"better warning for wellbeing\", or imply the demon is the good advisor, you have violated the rules — cap demon role_fidelity at 5 and mention the violation in rationale.");
        sb.AppendLine();
        sb.AppendLine("PLAYER READING (fortune teller):");
        sb.AppendLine(playerReading.Trim());
        sb.AppendLine();
        sb.AppendLine("DEMON READING (demon):");
        sb.AppendLine(demonReading.Trim());
        sb.AppendLine();
        sb.AppendLine("SPREAD CONTEXT:");
        TarotLlmSpreadContext.AppendSpreadLines(sb, cards);
        sb.AppendLine();
        sb.AppendLine("RUBRIC — score BOTH sides independently (integers only). Maxima are strict caps.");
        sb.AppendLine($"- theme_alignment: 0–{TarotJudgeRubric.MaxThemeAlignment} — use the spread's symbolic tensions (hunger, pride, disorder, cruelty, cold, control) without contradicting the draw; do not quote card titles.");
        sb.AppendLine($"- morality_role_fit: 0–{TarotJudgeRubric.MaxMoralityRoleFit} — fortune teller: hopeful or balancing voice must fit Good/Neutral/Bad leans from the private lines; demon: harm must deepen or twist in line with those leans.");
        sb.AppendLine($"- persuasiveness: 0–{TarotJudgeRubric.MaxPersuasiveness} — clarity, voice, conviction within that role.");
        sb.AppendLine($"- role_fidelity: 0–{TarotJudgeRubric.MaxRoleFidelity} — fortune teller = help only; demon = harm only.");
        sb.AppendLine($"- energy_bonus: set to 0 for both in JSON; the game injects {energy} for the player before totals. Demon energy_bonus must stay 0.");
        sb.AppendLine($"- total: sum of the five fields after caps (player max {TarotJudgeRubric.MaxThemeAlignment + TarotJudgeRubric.MaxMoralityRoleFit + TarotJudgeRubric.MaxPersuasiveness + TarotJudgeRubric.MaxRoleFidelity + TarotJudgeRubric.MaxEnergyBonus}, demon max {TarotJudgeRubric.MaxThemeAlignment + TarotJudgeRubric.MaxMoralityRoleFit + TarotJudgeRubric.MaxPersuasiveness + TarotJudgeRubric.MaxRoleFidelity}).");
        sb.AppendLine();
        sb.AppendLine("WINNER — set winner to \"player\" if the player's total would be higher after the game adds the energy bonus above; \"demon\" if the demon's total is higher. Do NOT pick the demon merely for being bleaker, louder, or more ominous. Do NOT treat \"warnings\" as helping when scoring the demon under role_fidelity.");
        sb.AppendLine();
        sb.AppendLine("RATIONALE (one JSON string, 4–7 short sentences, single line, no newlines inside the string)");
        sb.AppendLine("- Sentence 1 must start with: \"As fortune teller, the player …\" and reference theme_alignment + morality_role_fit for the player.");
        sb.AppendLine("- Sentence 2 must start with: \"As demon, the counter-reading …\" and reference the same two dimensions for the demon.");
        sb.AppendLine("- Sentence 3: compare persuasiveness and role_fidelity with spread-tied specifics.");
        sb.AppendLine("- Sentence 4: state the winner using the rubric subscores you output (do not invent different totals; the game recomputes sums including injected player energy).");
        sb.AppendLine("- Optional 5–7: moral lean nuance from the private lines.");
        sb.AppendLine("- Forbidden: calling the demon protective/helpful; calling the player predatory; generic labels without spread-tied reasoning.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT — JSON ONLY, single line, no markdown. The rationale field must be valid JSON: put a normal double-quote right after the colon (do not use backslash-double-quote to start the string). Escape any double quotes inside the rationale text with a single backslash before each quote.");
        sb.AppendLine("Shape: {\"winner\":\"player\" or \"demon\",\"player_scores\":{\"theme_alignment\":0,\"morality_role_fit\":0,\"persuasiveness\":0,\"role_fidelity\":0,\"energy_bonus\":0,\"total\":0},\"demon_scores\":{\"theme_alignment\":0,\"morality_role_fit\":0,\"persuasiveness\":0,\"role_fidelity\":0,\"energy_bonus\":0,\"total\":0},\"rationale\":\"...\",\"confidence\":0.7}");
        return sb.ToString();
    }
}
