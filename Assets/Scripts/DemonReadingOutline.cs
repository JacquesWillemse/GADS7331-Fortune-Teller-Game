using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Pass-1 structured outline for <see cref="DemonTarotTwoPass"/> before the prose curse is written.
/// </summary>
[Serializable]
public class DemonOutlineRoot
{
    public DemonLineOutline line1;
    public DemonLineOutline line2;
    public DemonLineOutline line3;
    public string sentence5_moral_read_hint;
}

[Serializable]
public class DemonLineOutline
{
    public int line;
    public string theme_family;
    public string anchor_1;
    public string anchor_2;
}

/// <summary>
/// Extracts and validates JSON from the outline model (strips fences / chatter).
/// </summary>
public static class DemonReadingOutlineParser
{
    public static bool TryParse(string raw, out DemonOutlineRoot root) =>
        TryParse(raw, null, out root, out _);

    /// <param name="spread">When set, invalid theme/anchor fields are repaired from the drawn cards.</param>
    public static bool TryParse(
        string raw,
        IReadOnlyList<TarotCardData> spread,
        out DemonOutlineRoot root,
        out string failureReason)
    {
        root = null;
        failureReason = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            failureReason = "empty model reply";
            return false;
        }

        if (!TryExtractOutlineJson(raw, out string json))
        {
            failureReason = "no JSON object found";
            return false;
        }

        DemonOutlineRoot o = null;
        try
        {
            o = JsonUtility.FromJson<DemonOutlineRoot>(json);
        }
        catch (Exception e)
        {
            failureReason = "JsonUtility: " + e.Message;
        }

        if (o == null)
            TryRegexPopulateLines(json, ref o);

        if (o == null)
        {
            if (string.IsNullOrEmpty(failureReason))
                failureReason = "JsonUtility returned null";
            return false;
        }

        if (spread != null && spread.Count > 0)
            RepairFromSpread(o, spread);

        if (!AllLinesValid(o, out string lineFail))
        {
            failureReason = lineFail;
            return false;
        }

        if (string.IsNullOrWhiteSpace(o.sentence5_moral_read_hint))
            o.sentence5_moral_read_hint = "cruel net moral weather";

        root = o;
        return true;
    }

    /// <summary>Deterministic outline from spread when the model JSON cannot be parsed (still runs pass-2 prose).</summary>
    public static bool TrySynthesizeFromSpread(IReadOnlyList<TarotCardData> spread, out DemonOutlineRoot root)
    {
        root = null;
        if (spread == null || spread.Count == 0)
            return false;

        var o = new DemonOutlineRoot();
        int n = spread.Count > 3 ? 3 : spread.Count;
        o.line1 = BuildLineFromCard(spread[0], 1);
        o.line2 = n > 1 ? BuildLineFromCard(spread[1], 2) : BuildLineFromCard(spread[0], 2);
        o.line3 = n > 2 ? BuildLineFromCard(spread[2], 3) : BuildLineFromCard(spread[Mathf.Max(0, n - 1)], 3);
        o.sentence5_moral_read_hint = "cruel net moral weather";
        root = o;
        return AllLinesValid(o, out _);
    }

    static DemonLineOutline BuildLineFromCard(TarotCardData card, int lineIndex)
    {
        string family = ThemeFamilyFromCard(card);
        DefaultAnchors(family, out string a1, out string a2);
        return new DemonLineOutline
        {
            line = lineIndex,
            theme_family = family,
            anchor_1 = a1,
            anchor_2 = a2
        };
    }

    static bool TryExtractOutlineJson(string raw, out string json)
    {
        json = null;
        string t = raw.Trim();
        t = Regex.Replace(t, @"```(?:json)?\s*", "", RegexOptions.IgnoreCase);
        t = t.Replace("```", "").Trim();

        if (TarotLlmJsonHelpers.TryExtractJsonObject(t, out json))
            return true;

        int start = t.IndexOf('{');
        int end = t.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            json = t.Substring(start, end - start + 1);
            return true;
        }

        return false;
    }

    static void TryRegexPopulateLines(string json, ref DemonOutlineRoot o)
    {
        if (string.IsNullOrEmpty(json))
            return;
        o ??= new DemonOutlineRoot();
        if (o.line1 == null)
            o.line1 = TryRegexLine(json, 1);
        if (o.line2 == null)
            o.line2 = TryRegexLine(json, 2);
        if (o.line3 == null)
            o.line3 = TryRegexLine(json, 3);

        if (string.IsNullOrEmpty(o.sentence5_moral_read_hint))
            o.sentence5_moral_read_hint = RegexField(json, "sentence5_moral_read_hint");
    }

    static DemonLineOutline TryRegexLine(string json, int lineNum)
    {
        string blockKey = "line" + lineNum;
        string theme = RegexFieldInBlock(json, blockKey, "theme_family");
        string a1 = RegexFieldInBlock(json, blockKey, "anchor_1");
        string a2 = RegexFieldInBlock(json, blockKey, "anchor_2");
        if (string.IsNullOrEmpty(theme) && string.IsNullOrEmpty(a1) && string.IsNullOrEmpty(a2))
            return null;

        string family = NormalizeThemeFamily(theme);
        if (family == null)
            return null;

        SanitizeAnchor(ref a1);
        SanitizeAnchor(ref a2);
        if (string.IsNullOrEmpty(a1) || string.IsNullOrEmpty(a2))
            DefaultAnchors(family, out a1, out a2);

        return new DemonLineOutline
        {
            line = lineNum,
            theme_family = family,
            anchor_1 = a1,
            anchor_2 = a2
        };
    }

    static string RegexField(string json, string key)
    {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    static string RegexFieldInBlock(string json, string blockKey, string fieldKey)
    {
        var block = Regex.Match(
            json,
            "\"" + Regex.Escape(blockKey) + "\"\\s*:\\s*\\{([^{}]*)\\}",
            RegexOptions.IgnoreCase);
        if (!block.Success)
            return RegexField(json, fieldKey);
        return RegexField(block.Groups[1].Value, fieldKey);
    }

    static void RepairFromSpread(DemonOutlineRoot o, IReadOnlyList<TarotCardData> spread)
    {
        int n = spread.Count > 3 ? 3 : spread.Count;
        o.line1 = RepairLine(o.line1, 1, spread[0]);
        if (n > 1)
            o.line2 = RepairLine(o.line2, 2, spread[1]);
        if (n > 2)
            o.line3 = RepairLine(o.line3, 3, spread[2]);
    }

    static DemonLineOutline RepairLine(DemonLineOutline line, int index, TarotCardData card)
    {
        line ??= new DemonLineOutline();
        line.line = index;

        string fromCard = ThemeFamilyFromCard(card);
        string normalized = NormalizeThemeFamily(line.theme_family);
        line.theme_family = normalized ?? fromCard;

        SanitizeAnchor(ref line.anchor_1);
        SanitizeAnchor(ref line.anchor_2);
        if (string.IsNullOrEmpty(line.anchor_1) || string.IsNullOrEmpty(line.anchor_2))
            DefaultAnchors(line.theme_family, out line.anchor_1, out line.anchor_2);

        return line;
    }

    static bool AllLinesValid(DemonOutlineRoot o, out string reason)
    {
        reason = null;
        if (o == null)
        {
            reason = "root is null";
            return false;
        }

        if (!LineOk(o.line1, 1, out reason))
            return false;
        if (!LineOk(o.line2, 2, out reason))
            return false;
        if (!LineOk(o.line3, 3, out reason))
            return false;
        return true;
    }

    static bool LineOk(DemonLineOutline L, int expectedLine, out string reason)
    {
        reason = null;
        if (L == null)
        {
            reason = "line" + expectedLine + " missing";
            return false;
        }

        SanitizeAnchor(ref L.anchor_1);
        SanitizeAnchor(ref L.anchor_2);
        if (string.IsNullOrWhiteSpace(L.anchor_1) || string.IsNullOrWhiteSpace(L.anchor_2))
        {
            reason = "line" + expectedLine + " anchors empty";
            return false;
        }

        if (NormalizeThemeFamily(L.theme_family) == null)
        {
            reason = "line" + expectedLine + " theme_family invalid: \"" + (L.theme_family ?? "") + "\"";
            return false;
        }

        L.theme_family = NormalizeThemeFamily(L.theme_family);
        if (L.line != 0 && L.line != expectedLine)
            L.line = expectedLine;
        return true;
    }

    static string ThemeFamilyFromCard(TarotCardData card)
    {
        string proper = DemonTarotPrompts.ThemeProperNounForCard(card);
        return proper.ToLowerInvariant();
    }

    /// <summary>Maps greed | vanity | chaos | power; accepts Greed, "Greed lane", etc.</summary>
    public static string NormalizeThemeFamily(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        string t = raw.Trim().ToLowerInvariant();
        if (t == "greed" || t.Contains("greed"))
            return "greed";
        if (t == "vanity" || t.Contains("vanity"))
            return "vanity";
        if (t == "chaos" || t.Contains("chaos"))
            return "chaos";
        if (t == "power" || t.Contains("power"))
            return "power";
        return null;
    }

    static void SanitizeAnchor(ref string anchor)
    {
        if (string.IsNullOrWhiteSpace(anchor))
            return;
        anchor = anchor.Trim().Trim('"', '\'');
        int space = anchor.IndexOf(' ');
        if (space > 0)
            anchor = anchor.Substring(0, space);
        if (anchor.Length > 32)
            anchor = anchor.Substring(0, 32);
    }

    static void DefaultAnchors(string themeFamily, out string a1, out string a2)
    {
        switch (themeFamily)
        {
            case "vanity":
                a1 = "glass";
                a2 = "mask";
                break;
            case "chaos":
                a1 = "dice";
                a2 = "hazard";
                break;
            case "power":
                a1 = "yoke";
                a2 = "command";
                break;
            default:
                a1 = "hunger";
                a2 = "maw";
                break;
        }
    }
}
