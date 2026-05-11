/// <summary>
/// If the LLM wrongly sets demon_agrees_with_player=true on a clearly hopeful reading,
/// force disagree so the duel can continue. Complements the gate prompt, not a full NLP model.
/// </summary>
public static class DemonGatePositiveHeuristic
{
    private static readonly string[] HopeSnippets =
    {
        "not bound",
        "let go",
        "prosper",
        "fulfill",
        "fulfil",
        "much fortune",
        "good fortune",
        "find fortune",
        "release",
        "at the release",
        "better life",
        "path forward",
        "you will find",
        "will find you",
        "fortune will",
        "luck will",
        "peace",
        "thrive",
        "there is hope",
        "hope remains",
        "uplift",
        "grow stronger",
        "heal",
        "forgive"
    };

    private static readonly string[] DoomSnippets =
    {
        "hopeless",
        "no hope",
        "irredeemable",
        "worthless",
        "deserve to suffer",
        "cursed forever",
        "beyond saving"
    };

    /// <summary>
    /// Returns true if the player text is clearly net-hopeful so the demon should NOT agree, even if the model said true.
    /// </summary>
    public static bool ShouldForceDemonDisagree(string playerReading)
    {
        if (string.IsNullOrWhiteSpace(playerReading))
            return false;

        string t = playerReading.ToLowerInvariant();
        foreach (string d in DoomSnippets)
        {
            if (t.Contains(d))
                return false;
        }

        int score = 0;
        foreach (string h in HopeSnippets)
        {
            if (t.Contains(h))
                score++;
        }

        if (t.Contains("misfortune"))
            score--;

        // "fortune" / blessings (typo-tolerant: goof fortune, etc.) — not already counted by longer phrases
        if (t.Contains("fortune") && !t.Contains("misfortune"))
        {
            if (!t.Contains("good fortune") && !t.Contains("much fortune") && !t.Contains("find fortune"))
                score++;
        }

        // Strong combo: release advice + blessing / outcome language (common positive closings)
        bool releaseThenBless =
            (t.Contains("let go") || t.Contains("release")) &&
            (t.Contains("will find you") || t.Contains("fortune will") || t.Contains("luck will") ||
             (t.Contains("fortune") && !t.Contains("misfortune")) || t.Contains("prosper"));

        // Strong combo: agency + prosperity wording
        bool combo = t.Contains("not bound") && (t.Contains("prosper") || (t.Contains("fortune") && !t.Contains("misfortune")));

        return score >= 2 || combo || releaseThenBless;
    }
}
