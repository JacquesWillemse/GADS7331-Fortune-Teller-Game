using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class DemonGateJson
{
    public bool demon_agrees_with_player;
    public string reason;
}

/// <summary>Per-side scores for the judge rubric (max 25+25+25+15 + energy on player only).</summary>
[Serializable]
public class JudgeSideScoresDto
{
    public int theme_alignment;
    public int morality_role_fit;
    public int persuasiveness;
    public int role_fidelity;
    public int energy_bonus;
    public int total;
}

[Serializable]
public class JudgeJson
{
    public string winner;
    public JudgeSideScoresDto player_scores;
    public JudgeSideScoresDto demon_scores;
    public string rationale;
    public float confidence;
}

/// <summary>
/// Best-effort JSON extraction from LLM replies (may include prose or fences).
/// </summary>
public static class TarotLlmJsonHelpers
{
    /// <summary>
    /// Finds the first JSON object in <paramref name="raw"/> by matching braces from its opening
    /// <c>{</c>, respecting strings so <c>}</c> inside rationale does not end the object early.
    /// Avoids <see cref="string.LastIndexOf(char)"/> on <c>}</c>, which breaks on truncated replies
    /// (last <c>}</c> may close <c>demon_scores</c> instead of the root).
    /// </summary>
    public static bool TryExtractJsonObject(string raw, out string json)
    {
        json = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string t = raw.Trim();
        int a = t.IndexOf('{');
        if (a < 0)
            return false;
        if (TryGetBalancedJsonEndInclusive(t, a, out int end))
        {
            json = t.Substring(a, end - a + 1);
            return true;
        }

        // Judge payloads often omit the final root "}". LastIndexOf('}') then closes at demon_scores and drops rationale.
        string tail = t.Substring(a);
        if (LooksLikeJudgeJsonFragment(tail))
        {
            string sealedTail = SealTrailingRootBrace(tail);
            if (TryGetBalancedJsonEndInclusive(sealedTail, 0, out int endSealed))
            {
                json = sealedTail.Substring(0, endSealed + 1);
                return true;
            }
        }

        // Truncated or malformed braces: last resort (may slice wrong if a "}" appears inside a string).
        int b = t.LastIndexOf('}');
        if (b > a)
        {
            json = t.Substring(a, b - a + 1);
            return true;
        }
        return false;
    }

    static bool LooksLikeJudgeJsonFragment(string tail)
    {
        if (string.IsNullOrEmpty(tail) || tail[0] != '{')
            return false;
        return tail.IndexOf("\"winner\"", StringComparison.OrdinalIgnoreCase) >= 0
            && tail.IndexOf("\"player_scores\"", StringComparison.OrdinalIgnoreCase) >= 0
            && tail.IndexOf("\"demon_scores\"", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Appends a single closing brace when the model stopped after "confidence": 0.9 with no root close.</summary>
    static string SealTrailingRootBrace(string tail)
    {
        string s = tail.TrimEnd();
        if (s.Length == 0)
            return tail;
        if (s[s.Length - 1] == '}')
            return s;
        return s + "\n}";
    }

    /// <summary>Index <paramref name="openBraceIndex"/> must point at <c>{</c>.</summary>
    public static bool TryGetBalancedJsonEndInclusive(string s, int openBraceIndex, out int endInclusive)
    {
        endInclusive = -1;
        if (s == null || openBraceIndex < 0 || openBraceIndex >= s.Length || s[openBraceIndex] != '{')
            return false;
        int depth = 0;
        bool inString = false;
        bool escape = false;
        for (int i = openBraceIndex; i < s.Length; i++)
        {
            char c = s[i];
            if (inString)
            {
                if (escape)
                    escape = false;
                else if (c == '\\')
                    escape = true;
                else if (c == '"')
                    inString = false;
            }
            else
            {
                if (c == '"')
                    inString = true;
                else if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        endInclusive = i;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static bool TryParseDemonGate(string raw, out DemonGateJson gate)
    {
        gate = null;
        if (!TryExtractJsonObject(raw, out string json))
            return false;
        try
        {
            gate = JsonUtility.FromJson<DemonGateJson>(json);
            return gate != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseJudge(string raw, out JudgeJson judge)
    {
        judge = null;
        string trimmedRaw = (raw ?? "").Trim();
        if (trimmedRaw.Length == 0)
            return false;

        if (!TryExtractJsonObject(trimmedRaw, out string json))
            return TryParseJudgeFromScoreListing(trimmedRaw, out judge);

        JudgeJson parsed = null;
        try
        {
            parsed = JsonUtility.FromJson<JudgeJson>(json);
        }
        catch
        {
            parsed = null;
        }

        if (parsed == null)
            parsed = new JudgeJson();

        // JsonUtility often leaves nested score objects null; fill from loose parse when needed.
        if (parsed.player_scores == null || parsed.demon_scores == null)
        {
            if (TryParseJudgeScoreObjects(json, out JudgeSideScoresDto ps, out JudgeSideScoresDto ds))
            {
                parsed.player_scores = ps;
                parsed.demon_scores = ds;
            }
        }

        if (string.IsNullOrEmpty(parsed.winner))
            parsed.winner = TryParseJudgeWinnerString(json);

        if (!string.IsNullOrEmpty(parsed.rationale))
        {
            string t = parsed.rationale.TrimStart();
            if (t.StartsWith("{", StringComparison.Ordinal) ||
                t.IndexOf("\"winner\"", StringComparison.OrdinalIgnoreCase) >= 0)
                parsed.rationale = null;
        }

        if (string.IsNullOrEmpty(parsed.rationale))
            parsed.rationale = TryParseJudgeRationaleString(json);
        if (string.IsNullOrEmpty(parsed.rationale))
            parsed.rationale = TryParseJudgeRationaleString(trimmedRaw);

        bool hasWinner = !string.IsNullOrEmpty(parsed.winner);
        bool hasScores = parsed.player_scores != null && parsed.demon_scores != null;
        if (!hasWinner && !hasScores)
            return false;

        judge = parsed;
        return true;
    }

    /// <summary>
    /// Models sometimes reply with prose + markdown lists ("Player:" / "Demon:") instead of JSON.
    /// Recovers subscores when each block lists theme_alignment, morality_role_fit, persuasiveness, role_fidelity.
    /// </summary>
    static bool TryParseJudgeFromScoreListing(string raw, out JudgeJson judge)
    {
        judge = null;
        if (string.IsNullOrEmpty(raw))
            return false;

        const RegexOptions lineOpts = RegexOptions.IgnoreCase | RegexOptions.Multiline;
        MatchCollection demonMatches = Regex.Matches(raw, @"^\s*Demon\s*:", lineOpts);
        MatchCollection playerMatches = Regex.Matches(raw, @"^\s*Player\s*:", lineOpts);
        if (demonMatches.Count == 0 || playerMatches.Count == 0)
            return false;

        int demonHeaderIdx = demonMatches[0].Index;
        int playerHeaderIdx = -1;
        for (int i = playerMatches.Count - 1; i >= 0; i--)
        {
            if (playerMatches[i].Index < demonHeaderIdx)
            {
                playerHeaderIdx = playerMatches[i].Index;
                break;
            }
        }

        string playerBody;
        string demonBody;
        int rationaleCut;

        if (playerHeaderIdx >= 0)
        {
            rationaleCut = Math.Min(playerHeaderIdx, demonHeaderIdx);
            int pContent = IndexAfterLineStart(raw, playerHeaderIdx);
            demonBody = raw.Substring(IndexAfterLineStart(raw, demonHeaderIdx)).TrimEnd();
            playerBody = raw.Substring(pContent, demonHeaderIdx - pContent);
        }
        else
        {
            int playerAfterDemon = -1;
            for (int i = 0; i < playerMatches.Count; i++)
            {
                if (playerMatches[i].Index > demonHeaderIdx)
                {
                    playerAfterDemon = playerMatches[i].Index;
                    break;
                }
            }

            if (playerAfterDemon < 0)
                return false;

            rationaleCut = demonHeaderIdx;
            int dContent = IndexAfterLineStart(raw, demonHeaderIdx);
            demonBody = raw.Substring(dContent, playerAfterDemon - dContent);
            playerBody = raw.Substring(IndexAfterLineStart(raw, playerAfterDemon)).TrimEnd();
        }

        if (!TryParseJudgeSideFromListingBody(playerBody, out JudgeSideScoresDto ps))
            return false;
        if (!TryParseJudgeSideFromListingBody(demonBody, out JudgeSideScoresDto ds))
            return false;

        ps.energy_bonus = 0;
        ps.total = 0;
        ds.energy_bonus = 0;
        ds.total = 0;

        string rationale = BuildLooseJudgeListingRationale(raw, rationaleCut);
        judge = new JudgeJson
        {
            winner = "",
            player_scores = ps,
            demon_scores = ds,
            rationale = rationale,
            confidence = 0f,
        };
        return true;
    }

    static int IndexAfterLineStart(string raw, int lineStartIndex)
    {
        int n = raw.IndexOf('\n', lineStartIndex);
        return n < 0 ? raw.Length : n + 1;
    }

    static string BuildLooseJudgeListingRationale(string raw, int cutIndex)
    {
        if (cutIndex <= 0)
            return "(Scores recovered from judge listing; model did not return valid JSON.)";
        string t = raw.Substring(0, cutIndex).Trim();
        const int maxLen = 1800;
        if (t.Length > maxLen)
            t = t.Substring(0, maxLen).TrimEnd() + "…";
        return t.Length > 0
            ? t
            : "(Scores recovered from judge listing; model did not return valid JSON.)";
    }

    static bool TryParseJudgeSideFromListingBody(string body, out JudgeSideScoresDto dto)
    {
        dto = null;
        if (string.IsNullOrWhiteSpace(body))
            return false;
        var d = new JudgeSideScoresDto();
        if (!TryReadListingInt(body, "theme_alignment", out d.theme_alignment))
            return false;
        if (!TryReadListingInt(body, "morality_role_fit", out d.morality_role_fit))
            return false;
        if (!TryReadListingInt(body, "persuasiveness", out d.persuasiveness))
            return false;
        if (!TryReadListingInt(body, "role_fidelity", out d.role_fidelity))
            return false;
        dto = d;
        return true;
    }

    static bool TryReadListingInt(string body, string field, out int value)
    {
        value = 0;
        string esc = Regex.Escape(field);
        Match m = Regex.Match(
            body,
            $@"(?im)(?:^|\n)\s*[-*]?\s*{esc}\s*:\s*(-?\d+)",
            RegexOptions.None);
        if (!m.Success)
            return false;
        return int.TryParse(m.Groups[1].Value, out value);
    }

    static bool TryParseJudgeScoreObjects(string json, out JudgeSideScoresDto player, out JudgeSideScoresDto demon)
    {
        player = null;
        demon = null;
        if (!TryExtractObjectAfterKey(json, "player_scores", out string pJson))
            return false;
        if (!TryExtractObjectAfterKey(json, "demon_scores", out string dJson))
            return false;
        player = ParseJudgeSideScoresInner(pJson);
        demon = ParseJudgeSideScoresInner(dJson);
        return player != null && demon != null;
    }

    static bool TryExtractObjectAfterKey(string json, string key, out string innerObjectJson)
    {
        innerObjectJson = null;
        if (string.IsNullOrEmpty(json))
            return false;
        string needle = "\"" + key + "\"";
        int k = json.IndexOf(needle, StringComparison.Ordinal);
        if (k < 0)
            return false;
        int brace = json.IndexOf('{', k);
        if (brace < 0)
            return false;
        if (!TryGetBalancedJsonEndInclusive(json, brace, out int end))
            return false;
        innerObjectJson = json.Substring(brace, end - brace + 1);
        return true;
    }

    static JudgeSideScoresDto ParseJudgeSideScoresInner(string objJson)
    {
        var d = new JudgeSideScoresDto();
        if (!TryReadJsonIntField(objJson, "theme_alignment", out int te))
            return null;
        if (!TryReadJsonIntField(objJson, "morality_role_fit", out int mo))
            return null;
        if (!TryReadJsonIntField(objJson, "persuasiveness", out int pe))
            return null;
        if (!TryReadJsonIntField(objJson, "role_fidelity", out int ro))
            return null;
        TryReadJsonIntField(objJson, "energy_bonus", out int en);
        TryReadJsonIntField(objJson, "total", out int tot);
        d.theme_alignment = te;
        d.morality_role_fit = mo;
        d.persuasiveness = pe;
        d.role_fidelity = ro;
        d.energy_bonus = en;
        d.total = tot;
        return d;
    }

    static bool TryReadJsonIntField(string json, string key, out int value)
    {
        value = 0;
        string quoted = "\"" + key + "\"";
        int k = json.IndexOf(quoted, StringComparison.Ordinal);
        if (k < 0)
            return false;
        int colon = json.IndexOf(':', k + quoted.Length);
        if (colon < 0)
            return false;
        int i = colon + 1;
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
        int sign = 1;
        if (i < json.Length && json[i] == '-')
        {
            sign = -1;
            i++;
        }
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
        if (i >= json.Length || !char.IsDigit(json[i]))
            return false;
        int start = i;
        while (i < json.Length && char.IsDigit(json[i]))
            i++;
        if (!int.TryParse(json.Substring(start, i - start), out int n))
            return false;
        value = sign * n;
        return true;
    }

    static string TryParseJudgeWinnerString(string json)
    {
        const string key = "\"winner\"";
        int k = json.IndexOf(key, StringComparison.Ordinal);
        if (k < 0)
            return null;
        int colon = json.IndexOf(':', k + key.Length);
        if (colon < 0)
            return null;
        int q0 = json.IndexOf('"', colon + 1);
        if (q0 < 0)
            return null;
        int q1 = json.IndexOf('"', q0 + 1);
        if (q1 < 0)
            return null;
        return json.Substring(q0 + 1, q1 - q0 - 1);
    }

    /// <summary>
    /// Extracts the rationale string. Handles LLM output that uses <c>\"...\"</c> instead of valid JSON
    /// <c>"..."</c> after the colon.
    /// </summary>
    static string TryParseJudgeRationaleString(string json)
    {
        const string key = "\"rationale\"";
        int k = json.IndexOf(key, StringComparison.Ordinal);
        if (k < 0)
            return null;
        int colon = json.IndexOf(':', k + key.Length);
        if (colon < 0)
            return null;
        int i = colon + 1;
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
        if (i >= json.Length)
            return null;
        if (json[i] == '"')
            i++;
        else if (i + 1 < json.Length && json[i] == '\\' && json[i + 1] == '"')
            i += 2;
        else
            return null;

        var sb = new StringBuilder();
        bool escape = false;
        while (i < json.Length)
        {
            char c = json[i];
            if (escape)
            {
                sb.Append(c);
                escape = false;
                i++;
                continue;
            }
            if (c == '\\' && i + 1 < json.Length && json[i + 1] == '"')
            {
                // Always treat \" as a literal quote inside the rationale. A former early-exit here
                // truncated strings when \" was followed by whitespace and then "," (common in model output).
                sb.Append('"');
                i += 2;
                continue;
            }
            if (c == '"')
            {
                int j = i + 1;
                while (j < json.Length && char.IsWhiteSpace(json[j]))
                    j++;
                if (j >= json.Length || json[j] == ',' || json[j] == '}')
                    break;
                sb.Append(c);
                i++;
                continue;
            }
            sb.Append(c);
            i++;
        }
        string result = sb.ToString().Trim();
        return result.Length > 0 ? result : null;
    }

    /// <summary>Clamp subscores and sum. Demon energy_bonus should be 0; player may be 0–10.</summary>
    public static int SumJudgeSide(JudgeSideScoresDto s, int maxEnergyBonus)
    {
        if (s == null)
            return -1;
        int te = Mathf.Clamp(s.theme_alignment, 0, TarotJudgeRubric.MaxThemeAlignment);
        int mo = Mathf.Clamp(s.morality_role_fit, 0, TarotJudgeRubric.MaxMoralityRoleFit);
        int pe = Mathf.Clamp(s.persuasiveness, 0, TarotJudgeRubric.MaxPersuasiveness);
        int ro = Mathf.Clamp(s.role_fidelity, 0, TarotJudgeRubric.MaxRoleFidelity);
        int en = Mathf.Clamp(s.energy_bonus, 0, maxEnergyBonus);
        return te + mo + pe + ro + en;
    }

    public static bool IsPlayerWinner(string winnerField)
    {
        if (string.IsNullOrWhiteSpace(winnerField))
            return false;
        string w = winnerField.Trim().ToLowerInvariant();
        return w == "player" || w == "p" || w == "fortune_teller" || w == "teller";
    }
}
