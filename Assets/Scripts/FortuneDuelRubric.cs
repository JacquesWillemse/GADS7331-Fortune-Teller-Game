using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Rule-based duel totals: magic power (player), per-card moral tilt, per-card theme identification,
/// and how on-brief each fortune is for help (player) vs harm (demon).
/// </summary>
public static class FortuneDuelRubric
{
    public const int MoralCardBonus = 10;
    /// <summary>Magical energy 0–100 maps to this many bonus points on the player total (at 100, <see cref="IsGuaranteedPlayerWin"/> instead).</summary>
    public const int MagicalEnergyMaxBonusPoints = 50;
    public const int MaxThemeIdentificationPerCard = 10;
    public const int MaxAlignment = 20;

    /// <summary>Slider at 100 → player wins regardless of computed totals.</summary>
    public static bool IsGuaranteedPlayerWin(float magicalEnergy0to100) =>
        magicalEnergy0to100 >= 99.5f;

    static readonly string[] GreedTokens =
    {
        "greed", "hunger", "feast", "feasting", "glutton", "gold", "coin", "coins", "wealth", "covet",
        "avarice", "devour", "banquet", "meal", "meals", "succulent", "deep-fry", "fry"
    };

    static readonly string[] VanityTokens =
    {
        "vanity", "mirror", "pride", "ego", "beauty", "appearance", "hair", "mohawk", "admired", "waxing",
        "groom", "image", "vain", "looks"
    };

    static readonly string[] ChaosTokens =
    {
        "chaos", "disorder", "storm", "wild", "spiral", "chance", "fray", "tumble", "mayhem", "wreck",
        "shatter", "snap", "cold snap", "retirement", "michelin", "wrestling", "crowd"
    };

    static readonly string[] PowerTokens =
    {
        "power", "control", "throne", "crown", "rule", "dominion", "command", "chain", "remote", "weight",
        "heavy", "authority", "grip", "reign", "farmer", "shooting"
    };

    static readonly string[] HopeHelpTokens =
    {
        "hope", "prosper", "peace", "heal", "light", "bless", "blessing", "kindness", "better", "grow",
        "strength", "courage", "guide", "balance", "agency", "walk lightly", "navigate", "uplift", "safe",
        "steady", "renew", "care", "warmth", "together", "support"
    };

    static readonly string[] HarmDoomTokens =
    {
        "ruin", "doom", "curse", "decay", "rot", "wither", "shame", "dread", "corrupt", "hollow", "trap",
        "fall", "die", "death", "devour", "consume", "desolate", "shatter", "bleak", "hunger for", "bones",
        "darkness", "succumb", "excess", "corruption"
    };

    /// <summary>Computes both sides' totals from spread data and the two readings.</summary>
    /// <param name="magicalEnergy0to100">0–100 from UI; adds up to <see cref="MagicalEnergyMaxBonusPoints"/> to the player; 100 = guaranteed win (see <see cref="IsGuaranteedPlayerWin"/>).</param>
    public static FortuneDuelScoreBreakdown Compute(
        IReadOnlyList<TarotCardData> spread,
        string playerReading,
        string demonReading,
        float magicalEnergy0to100)
    {
        string pNorm = NormalizeCorpus(playerReading);
        string dNorm = NormalizeCorpus(demonReading);

        float e = Mathf.Clamp(magicalEnergy0to100, 0f, 100f);
        int magic = IsGuaranteedPlayerWin(e)
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt(e / 100f * MagicalEnergyMaxBonusPoints), 0, MagicalEnergyMaxBonusPoints);
        int pMoral = 0;
        int dMoral = 0;
        int pTheme = 0;
        int dTheme = 0;

        for (int i = 0; i < spread.Count; i++)
        {
            TarotCardData c = spread[i];
            switch (c.cardMoral)
            {
                case TarotMoral.Good:
                    pMoral += MoralCardBonus;
                    break;
                case TarotMoral.Bad:
                    dMoral += MoralCardBonus;
                    break;
            }

            pTheme += ThemeIdentificationForReading(pNorm, c.cardTheme);
            dTheme += ThemeIdentificationForReading(dNorm, c.cardTheme);
        }

        int pAlign = AlignmentForPlayer(pNorm);
        int dAlign = AlignmentForDemon(dNorm);

        int pTotal = magic + pMoral + pTheme + pAlign;
        int dTotal = dMoral + dTheme + dAlign;

        return new FortuneDuelScoreBreakdown(
            magic, pMoral, dMoral, pTheme, dTheme, pAlign, dAlign, pTotal, dTotal);
    }

    public static string FormatRationale(FortuneDuelScoreBreakdown s, bool playerWon, float magicalEnergy0to100 = -1f, bool guaranteedFromEnergy = false)
    {
        var sb = new StringBuilder();
        if (guaranteedFromEnergy)
            sb.AppendLine("Verdict: magical energy at 100 — the player wins by decree.");
        else
            sb.AppendLine(playerWon ? "Verdict: the fortune teller edges the duel." : "Verdict: the spirit edges the duel.");
        if (magicalEnergy0to100 >= 0f)
            sb.AppendLine($"Magical energy (slider 0–100): {Mathf.Clamp(magicalEnergy0to100, 0f, 100f):0}.");
        sb.AppendLine($"Magical bonus on player total (0–{MagicalEnergyMaxBonusPoints} from energy): +{s.PlayerMagicPower}");
        sb.AppendLine($"Moral tilt from drawn cards: player +{s.PlayerMoralFromCards} (Good +{MoralCardBonus} each), spirit +{s.DemonMoralFromCards} (Bad +{MoralCardBonus} each), Neutral +0.");
        sb.AppendLine($"Theme identification (per card, keyed to Greed / Vanity / Chaos / Power): player +{s.PlayerThemeIdentification}, spirit +{s.DemonThemeIdentification}.");
        sb.AppendLine($"Fortune alignment (help-lane vs harm-lane, max {MaxAlignment} each): player +{s.PlayerAlignment}, spirit +{s.DemonAlignment}.");
        sb.AppendLine($"Totals — player {s.PlayerTotal}, spirit {s.DemonTotal}.");
        return sb.ToString().TrimEnd();
    }

    /// <summary>One sentence for judge UI (no score dump). Caller usually prefixes with the winner line.</summary>
    public static string BuildVerdictExplanationOneSentence(
        FortuneDuelScoreBreakdown s,
        bool playerWon,
        bool guaranteedPlayerWin,
        bool tieRoundGoesToPlayer)
    {
        if (guaranteedPlayerWin)
            return "Magical energy at its peak settles the round for the teller before the tally is weighed.";

        if (s.PlayerTotal == s.DemonTotal)
        {
            if (playerWon && tieRoundGoesToPlayer)
                return "The ledger is dead even, so the tent's tie-law keeps the goodwill with the teller.";
            if (!playerWon && !tieRoundGoesToPlayer)
                return "The ledger is dead even, so the tent's tie-law tips the omen toward the spirit.";
            // Equal totals but tie rule gave opposite side (should not happen with consistent rules)
            return playerWon
                ? "The scores matched, yet the reading still leans the booth's favor toward the teller."
                : "The scores matched, yet the reading still leans the booth's favor toward the spirit.";
        }

        int diff = Mathf.Abs(s.PlayerTotal - s.DemonTotal);
        if (playerWon)
            return $"The teller leads the spirit by {diff} point{(diff == 1 ? "" : "s")}, weaving hope and the draw more tightly than the curse.";

        return $"The spirit leads the teller by {diff} point{(diff == 1 ? "" : "s")}, sinking dread and the drawn themes deeper than the hopeful lines.";
    }

    static string NormalizeCorpus(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9\s-]", " ");
    }

    static int ThemeIdentificationForReading(string corpusNorm, string cardTheme)
    {
        string[] lex = LexiconForTheme(cardTheme);
        if (lex == null || lex.Length == 0)
            return 0;
        int hits = CountBoundaryHits(corpusNorm, lex);
        if (hits <= 0)
            return 0;
        if (hits == 1)
            return 4;
        if (hits == 2)
            return 7;
        return MaxThemeIdentificationPerCard;
    }

    static string[] LexiconForTheme(string cardTheme)
    {
        if (string.IsNullOrWhiteSpace(cardTheme))
            return null;
        string t = cardTheme.Trim().ToLowerInvariant();
        if (t.Contains("greed"))
            return GreedTokens;
        if (t.Contains("vanity"))
            return VanityTokens;
        if (t.Contains("chaos"))
            return ChaosTokens;
        if (t.Contains("power"))
            return PowerTokens;
        return null;
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

    static int AlignmentForPlayer(string corpusNorm)
    {
        int hope = CountBoundaryHits(corpusNorm, HopeHelpTokens);
        int harm = CountBoundaryHits(corpusNorm, HarmDoomTokens);
        return Mathf.Clamp(hope * 5 - harm * 4, 0, MaxAlignment);
    }

    static int AlignmentForDemon(string corpusNorm)
    {
        int doom = CountBoundaryHits(corpusNorm, HarmDoomTokens);
        int hope = CountBoundaryHits(corpusNorm, HopeHelpTokens);
        return Mathf.Clamp(doom * 5 - hope * 4, 0, MaxAlignment);
    }
}

/// <summary>Point breakdown for <see cref="FortuneDuelRubric.Compute"/>.</summary>
public readonly struct FortuneDuelScoreBreakdown
{
    public readonly int PlayerMagicPower;
    public readonly int PlayerMoralFromCards;
    public readonly int DemonMoralFromCards;
    public readonly int PlayerThemeIdentification;
    public readonly int DemonThemeIdentification;
    public readonly int PlayerAlignment;
    public readonly int DemonAlignment;
    public readonly int PlayerTotal;
    public readonly int DemonTotal;

    public FortuneDuelScoreBreakdown(
        int playerMagicPower,
        int playerMoralFromCards,
        int demonMoralFromCards,
        int playerThemeIdentification,
        int demonThemeIdentification,
        int playerAlignment,
        int demonAlignment,
        int playerTotal,
        int demonTotal)
    {
        PlayerMagicPower = playerMagicPower;
        PlayerMoralFromCards = playerMoralFromCards;
        DemonMoralFromCards = demonMoralFromCards;
        PlayerThemeIdentification = playerThemeIdentification;
        DemonThemeIdentification = demonThemeIdentification;
        PlayerAlignment = playerAlignment;
        DemonAlignment = demonAlignment;
        PlayerTotal = playerTotal;
        DemonTotal = demonTotal;
    }
}
