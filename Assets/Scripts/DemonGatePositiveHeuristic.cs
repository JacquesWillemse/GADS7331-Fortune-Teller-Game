/// <summary>
/// If the LLM wrongly sets demon_agrees_with_player=true on a clearly hopeful reading,
/// force disagree so the duel can continue. Complements the gate prompt, not a full NLP model.
/// </summary>
public static class DemonGatePositiveHeuristic
{
    /// <summary>
    /// One match is enough to treat the reading as net-positive for gate correction (high-precision phrases).
    /// </summary>
    private static readonly string[] StrongHopePhrases =
    {
        "happy life",
        "happily ever after",
        "live happily",
        "overcoming adversity",
        "overcome adversity",
        "triumph over",
        "path to happiness",
        "brighter future",
        "bright future",
        "good life ahead",
        "better days ahead",
        "you will be happy",
        "will be happy",
        "everything will be okay",
        "everything will be ok",
        "things will get better",
        "worth living",
        "blessings await",
        "joy awaits",
        "victory over",
        "successful outcome",
        "positive outcome",
        "hope after",
        "hope and healing",
        "healing and hope",
        "land on hope",
        "lands on hope",
        "ends in hope",
        "uplifting close",
        "positive close"
    };

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
        "forgive",
        "happy ending",
        "find joy",
        "find peace",
        "inner peace",
        "self-love",
        "self love",
        "you deserve happiness",
        "deserve happiness",
        "worthy of love",
        "new beginning",
        "fresh start",
        "you can heal",
        "you will heal",
        "you will thrive",
        "abundance",
        "gratitude",
        "blessed",
        "blessing",
        "triumph",
        "victory",
        "success awaits",
        "succeed",
        "prosperity",
        "fulfillment",
        "fulfilment",
        "contentment",
        "serenity",
        "harmony",
        "reconciliation",
        "redemption arc",
        "second chance",
        "light at the end",
        "silver lining",
        "rising above",
        "rise above",
        "emerge stronger",
        "stronger than",
        "love wins",
        "love conquers"
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
    /// When the model sets demon_agrees_with_player=true but its own <paramref name="gateReason"/> admits uplift,
    /// a happy outcome, overcoming, etc., treat as disagree (common LLM slip).
    /// </summary>
    public static bool ModelGateReasonAdmitsNetPositive(string gateReason)
    {
        if (string.IsNullOrWhiteSpace(gateReason))
            return false;

        string r = gateReason.ToLowerInvariant();
        if (r.Contains("hopeless") || r.Contains("no hope") || r.Contains("irredeemable"))
            return false;

        string[] markers =
        {
            "happy life",
            "happier",
            "overcoming adversity",
            "implies overcoming",
            "path forward",
            "net helpful",
            "net positive",
            "net-positive",
            "reassuring",
            "uplifting",
            "good outcome",
            "better life",
            "find peace",
            "inner strength",
            "resilience",
            "prosper",
            "fulfillment",
            "peace after",
            "still hopeful",
            "hope remains",
            "agency",
            "positive framing",
            "hope after",
            "hope and",
            "ends in hope",
            "lands on hope",
            "positive close",
            "joy awaits",
            "blessing"
        };

        foreach (string m in markers)
        {
            if (r.Contains(m))
                return true;
        }

        return false;
    }

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

        foreach (string s in StrongHopePhrases)
        {
            if (t.Contains(s))
                return true;
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
