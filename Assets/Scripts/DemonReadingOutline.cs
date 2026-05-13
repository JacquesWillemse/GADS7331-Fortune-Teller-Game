using System;
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
    public static bool TryParse(string raw, out DemonOutlineRoot root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        string json = raw.Substring(start, end - start + 1);
        try
        {
            DemonOutlineRoot o = JsonUtility.FromJson<DemonOutlineRoot>(json);
            if (o == null || o.line1 == null || o.line2 == null || o.line3 == null)
                return false;
            if (!LineOk(o.line1, 1) || !LineOk(o.line2, 2) || !LineOk(o.line3, 3))
                return false;
            root = o;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool LineOk(DemonLineOutline L, int expectedLine)
    {
        if (L == null)
            return false;
        if (string.IsNullOrWhiteSpace(L.anchor_1) || string.IsNullOrWhiteSpace(L.anchor_2))
            return false;
        string t = (L.theme_family ?? "").Trim().ToLowerInvariant();
        if (t != "greed" && t != "vanity" && t != "chaos" && t != "power")
            return false;
        return L.line == 0 || L.line == expectedLine;
    }
}
