using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Customer wealth for duel prompts and <see cref="FortuneDuelRubric"/> wealth-fit scoring.
/// </summary>
public static class FortuneClientWealthContext
{
    public const int MaxWealthFit = 15;

    static readonly string[] RichFitTokens =
    {
        "wealthy", "wealth", "affluent", "luxury", "silk", "velvet", "gilded", "gold", "estate", "heir",
        "patron", "prosper", "fortune", "coins", "coin", "inheritance", "carriage", "banquet", "refined",
        "privilege", "comfort", "cushion", "lavish", "estate", "inherit", "silver", "jewel", "jewels"
    };

    static readonly string[] RichMismatchTokens =
    {
        "rags", "penniless", "destitute", "beggar", "barrel", "scraping", "hollow belly", "thin purse",
        "patch", "frayed", "thrift", "worn shoes", "starving", "starve", "homeless"
    };

    static readonly string[] PoorFitTokens =
    {
        "humble", "scrape", "scraping", "thrift", "worn", "weary", "bread", "rent", "modest", "need",
        "hunger", "penny", "patch", "dignity", "survival", "thin", "frayed", "barrel", "patched",
        "hollow belly", "thin purse", "lean", "threadbare", "mend", "mending", "coinless"
    };

    static readonly string[] PoorMismatchTokens =
    {
        "yacht", "gilded", "heir", "estate", "velvet", "monocle", "banquet", "silk", "limousine",
        "trust fund", "caviar", "inheritance", "carriage", "lavish", "affluent"
    };

    public static string LabelFor(FortuneClientSpawner.WealthType wealth) =>
        wealth == FortuneClientSpawner.WealthType.Rich ? "wealthy" : "poor";

    public static string CustomerStationFor(FortuneClientSpawner.WealthType wealth) =>
        wealth == FortuneClientSpawner.WealthType.Rich
            ? "a wealthy customer (comfort, privilege, gilded habits — not a list of props from the scene)"
            : "a poor customer (scrape, thrift, worn dignity — not a list of props from the scene)";

    public static void AppendClientBlock(StringBuilder sb, FortuneClientSpawner.WealthType wealth)
    {
        sb.AppendLine("CUSTOMER WEALTH (mandatory context for this reading):");
        sb.Append("- The customer before the booth is **").Append(LabelFor(wealth)).AppendLine("**.");
        if (wealth == FortuneClientSpawner.WealthType.Rich)
        {
            sb.AppendLine("- Readings should **speak to their station**: privilege, cushion, inheritance, gilded appetite, rot beneath refinement — not generic poverty imagery.");
            sb.AppendLine("- A fortune teller who treats them like a beggar misses the booth; a spirit curse should sour **excess and entitlement**.");
        }
        else
        {
            sb.AppendLine("- Readings should **speak to their station**: scrape, rent, thin purse, patched dignity, hunger dressed as patience — not yacht-and-heir fantasy.");
            sb.AppendLine("- A fortune teller who flatters them with luxury fiction misses the booth; a spirit curse should kick **when they are already ground down**.");
        }
        sb.AppendLine("- Do **not** name 3D props (hats, barrels, glasses); speak to **wealth or poverty** only.");
        sb.AppendLine();
    }

    public static void AppendPlayerCoachBlock(StringBuilder sb, FortuneClientSpawner.WealthType wealth)
    {
        sb.AppendLine("PLAYER COACH (fortune teller — not spoken by the spirit):");
        sb.Append("- Customer is **").Append(LabelFor(wealth)).AppendLine("** — weave their station into hope or mercy.");
        sb.AppendLine("- For each card, **echo the vignette loosely** (image, verb, appetite) without pasting the title.");
        sb.AppendLine("- Name each card's **theme** (Greed / Vanity / Chaos / Power) and twist printed **moral lean** toward hope.");
        sb.AppendLine();
    }

    public static int ScoreWealthFitForPlayer(string corpusNorm, FortuneClientSpawner.WealthType wealth)
    {
        if (wealth == FortuneClientSpawner.WealthType.Rich)
            return ScoreWealthFit(corpusNorm, RichFitTokens, RichMismatchTokens);
        return ScoreWealthFit(corpusNorm, PoorFitTokens, PoorMismatchTokens);
    }

    public static int ScoreWealthFitForDemon(string corpusNorm, FortuneClientSpawner.WealthType wealth)
    {
        if (wealth == FortuneClientSpawner.WealthType.Rich)
            return ScoreWealthFit(corpusNorm, RichFitTokens, RichMismatchTokens);
        return ScoreWealthFit(corpusNorm, PoorFitTokens, PoorMismatchTokens);
    }

    static int ScoreWealthFit(string corpusNorm, string[] fitTokens, string[] mismatchTokens)
    {
        if (string.IsNullOrEmpty(corpusNorm))
            return 0;
        int fit = CountBoundaryHits(corpusNorm, fitTokens);
        int wrong = CountBoundaryHits(corpusNorm, mismatchTokens);
        return System.Math.Max(0, System.Math.Min(MaxWealthFit, fit * 4 - wrong * 3));
    }

    static int CountBoundaryHits(string corpusNorm, IEnumerable<string> tokens)
    {
        int n = 0;
        foreach (string tok in tokens)
        {
            if (string.IsNullOrEmpty(tok) || tok.Length < 2)
                continue;
            try
            {
                if (Regex.IsMatch(corpusNorm, @"\b" + Regex.Escape(tok) + @"\b", RegexOptions.IgnoreCase))
                    n++;
            }
            catch
            {
                // ignore bad escape edge cases
            }
        }
        return n;
    }
}
