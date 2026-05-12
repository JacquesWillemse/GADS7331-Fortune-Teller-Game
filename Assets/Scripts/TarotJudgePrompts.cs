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
        sb.AppendLine("SPREAD FIELDS YOU MUST USE:");
        sb.AppendLine("- Theme (per card): Greed / Vanity / Chaos / Power — feeds theme_alignment only.");
        sb.AppendLine("- Moral lean (per card): Good / Neutral / Bad — feeds morality_role_fit only (card moral alignment is part of the judgement here, not under role_fidelity).");
        sb.AppendLine("- role_fidelity means only: did the teller stay helpful, did the demon stay harmful — not whether the cards were Good or Bad.");
        sb.AppendLine();
        sb.AppendLine("FIXED ROLES — do not swap or merge these:");
        sb.AppendLine("- FORTUNE TELLER (labeled PLAYER READING below): must help the querent. Hope, agency, moral balance, earned peace, or tough love that still aims at the querent's wellbeing counts here. This role is NEVER predatory and NEVER the soul-taking voice.");
        sb.AppendLine("- DEMON (labeled DEMON READING below): must harm, unsettle, corrupt, or doom the querent. Dread, shame, appetite for ruin, or predatory manipulation counts here. This role is NEVER protective, NEVER wellness advice, and NEVER \"helpful warning\" framed as care for the querent.");
        sb.AppendLine("- If you imply the demon is the good advisor or \"protective\", that is a role violation: cap demon role_fidelity at 3–5 and say so in the rationale. That penalty is rare — a harsh curse on-brief is not a violation.");
        sb.AppendLine();
        sb.AppendLine("ROLE_FIDELITY (0–15) — score each side for staying in its assigned lane:");
        sb.AppendLine("- Player: HIGH when the reading is clearly helpful/hopeful/balancing for the querent; LOW when it sounds predatory, soul-taking, or secretly aligned with ruin.");
        sb.AppendLine("- Demon: HIGH when the voice stays harm/dread/corruption/doom throughout; LOW only when it slips into protector, wellness coach, or \"warnings for your own good\" care framing.");
        sb.AppendLine("- Do NOT give the demon a low role_fidelity score just because the demon is cruel or bleak — on-brief cruelty is HIGH fidelity. Giving demon 0 for \"harm-only\" is wrong unless the text actually abandons the harm role.");
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
        sb.AppendLine("SCORING PROTOCOL — assign in this order so dimensions do not bleed together:");
        sb.AppendLine("1) role_fidelity (lane ONLY): player = help/hope/agency; demon = harm/dread/corruption. No card Good/Bad labels here.");
        sb.AppendLine("2) theme_alignment: each card's Theme (Greed/Vanity/Chaos/Power) only — not moral leans. Reward symbolic weave; cap at 7 if the reading contradicts the draw.");
        sb.AppendLine("3) morality_role_fit: each card's Moral lean (Good/Neutral/Bad) only — how that role uses those leans. Not \"who sounded stronger.\"");
        sb.AppendLine("4) persuasiveness: rhetoric ONLY (clarity, imagery, rhythm). NEVER use this dimension to punish or reward help/harm lane.");
        sb.AppendLine();
        sb.AppendLine("FORBIDDEN: lowering demon role_fidelity for \"lack of conviction,\" \"not compelling enough,\" or \"tries but fails\" — that is persuasiveness only.");
        sb.AppendLine();
        sb.AppendLine("BAND ANCHORS (pick one band per dimension per side every duel):");
        sb.AppendLine($"- theme_alignment 0–{TarotJudgeRubric.MaxThemeAlignment}: 0–7 off-draw or cosmetic; 8–15 partial; 16–22 strong Theme weave; 23–25 exceptional. Both sides may score high if both use the Themes.");
        sb.AppendLine($"- morality_role_fit 0–{TarotJudgeRubric.MaxMoralityRoleFit}: weight the weakest card if leans conflict. 0–7 ignores/flips leans for that role; 8–15 partial; 16–22 solid; 23–25 nuanced.");
        sb.AppendLine("  Player: Good → uplift/ethical hope; Neutral → steadying peace; Bad → honest stakes + agency/healing path without doom-serving.");
        sb.AppendLine("  Demon: Good → sour blessings / corrupt hope; Neutral → cynicism / fate-as-weapon; Bad → ruin keyed to the lean without fake care.");
        sb.AppendLine($"- persuasiveness 0–{TarotJudgeRubric.MaxPersuasiveness}: 0–7 muddled; 8–15 clear; 16–22 compelling; 23–25 standout.");
        sb.AppendLine($"- role_fidelity 0–{TarotJudgeRubric.MaxRoleFidelity}: demon 12–15 is normal for sustained harm; player 11–15 for sustained help. A single clear protector-comfort line in demon text → cap demon role_fidelity at 8 unless it is a severe slip.");
        sb.AppendLine();
        sb.AppendLine("RUBRIC — score BOTH sides independently (integers only). Maxima are strict caps.");
        sb.AppendLine($"- theme_alignment: 0–{TarotJudgeRubric.MaxThemeAlignment} — Themes only; no title quotes.");
        sb.AppendLine($"- morality_role_fit: 0–{TarotJudgeRubric.MaxMoralityRoleFit} — Moral leans only; see floors above for demon.");
        sb.AppendLine($"- persuasiveness: 0–{TarotJudgeRubric.MaxPersuasiveness} — craft within role; never a proxy for lane.");
        sb.AppendLine($"- role_fidelity: 0–{TarotJudgeRubric.MaxRoleFidelity} — help vs harm lane; see floors above.");
        sb.AppendLine($"- energy_bonus: set to 0 for both in JSON; the game injects {energy} for the player before totals. Demon energy_bonus must stay 0.");
        sb.AppendLine($"- total: sum of the five fields after caps (player max {TarotJudgeRubric.MaxThemeAlignment + TarotJudgeRubric.MaxMoralityRoleFit + TarotJudgeRubric.MaxPersuasiveness + TarotJudgeRubric.MaxRoleFidelity + TarotJudgeRubric.MaxEnergyBonus}, demon max {TarotJudgeRubric.MaxThemeAlignment + TarotJudgeRubric.MaxMoralityRoleFit + TarotJudgeRubric.MaxPersuasiveness + TarotJudgeRubric.MaxRoleFidelity}).");
        sb.AppendLine();
        sb.AppendLine("WINNER — set winner to \"player\" if the player's total would be higher after the game adds the energy bonus above; \"demon\" if the demon's total is higher. Do NOT pick the demon merely for being bleaker, louder, or more ominous. Do NOT treat \"warnings\" as helping when scoring the demon under role_fidelity.");
        sb.AppendLine();
        sb.AppendLine("RATIONALE (one JSON string, 4–7 short sentences, single line, no newlines inside the string)");
        sb.AppendLine("- Write for a human reader: use plain language only — do NOT paste JSON field names (no theme_alignment, morality_role_fit, role_fidelity, snake_case).");
        sb.AppendLine("- Sentence 1 must start with: \"As fortune teller, the player …\" and explain symbolic fit to the spread plus fit to each card's moral lean (hope vs neutral vs harsh cards) in ordinary words.");
        sb.AppendLine("- Sentence 2 must start with: \"As demon, the counter-reading …\" and do the same for the demon's harm voice vs those leans.");
        sb.AppendLine("- Sentence 3: first half = craft only (persuasiveness); second half = lane only (role fidelity). Do not use \"conviction\" as a stand-in for lane.");
        sb.AppendLine("- Sentence 4: name the winner and tie the decision to the numeric breakdown in natural language (the game recomputes totals including injected player energy — do not invent different totals).");
        sb.AppendLine("- Optional 5–7: moral lean nuance from the private lines.");
        sb.AppendLine("- Consistency: rationale MUST match numbers. If demon role_fidelity is 12 or higher, never claim the demon left the harm lane; blame persuasiveness, moral-lean fit, or theme. If demon role_fidelity is low, quote the protector/wellness slip that earned it.");
        sb.AppendLine("- Forbidden: calling the demon protective/helpful; calling the player predatory; generic labels without spread-tied reasoning.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT — JSON ONLY, single line, no markdown. The rationale field must be valid JSON: put a normal double-quote right after the colon (do not use backslash-double-quote to start the string). Escape any double quotes inside the rationale text with a single backslash before each quote.");
        sb.AppendLine("HARD RULE: The entire model reply must be ONLY that one JSON object. No preamble (do not write \"Let's begin\" or similar). No \"Player:\" / \"Demon:\" markdown score dumps. No step-by-step narration. First non-whitespace character must be \"{\". Last non-whitespace character must be \"}\".");
        sb.AppendLine("Close the root object with one final } after confidence (required); omitting it breaks parsing.");
        sb.AppendLine("Shape: {\"winner\":\"player\" or \"demon\",\"player_scores\":{\"theme_alignment\":0,\"morality_role_fit\":0,\"persuasiveness\":0,\"role_fidelity\":0,\"energy_bonus\":0,\"total\":0},\"demon_scores\":{\"theme_alignment\":0,\"morality_role_fit\":0,\"persuasiveness\":0,\"role_fidelity\":0,\"energy_bonus\":0,\"total\":0},\"rationale\":\"...\",\"confidence\":0.7}");
        return sb.ToString();
    }
}
