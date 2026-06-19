using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Rule-based duel totals: magic power (player), per-card moral tilt from **printed** card leans (Good/Neutral/Bad),
/// per-card theme identification (abstract lane lexicon), loose vignette echo from card descriptions,
/// customer wealth fit, hope-vs-harm alignment, moral spread judge bias, and a small **all-Neutral draw** bonus.
/// Energy 0–100 adds player bonus; 100 = automatic teller win.
/// </summary>
public static class FortuneDuelRubric
{
    public const int MoralCardBonus = 10;
    /// <summary>When the spread has more Good than Bad cards, this many points go to the teller; vice versa for Bad over Good.</summary>
    public const int MoralSpreadJudgeBiasPoints = 15;
    /// <summary>Magical energy 0–100 maps to this many bonus points on the player total (at 100, <see cref="IsGuaranteedPlayerWin"/> instead).</summary>
    public const int MagicalEnergyMaxBonusPoints = 50;
    /// <summary>Ceiling per card for theme weave; curve saturates below old 10 to reduce thesaurus-padding wins.</summary>
    public const int MaxThemeIdentificationPerCard = 8;
    /// <summary>Ceiling per card for loose vignette echo (keywords from card description).</summary>
    public const int MaxDescriptionEchoPerCard = 6;
    public const int MaxAlignment = 20;
    /// <summary>When every drawn card is Neutral and the teller shows enough hope-language, the booth steadies the goodwill.</summary>
    public const int NeutralSpreadTellerBonus = 4;
    /// <summary>Minimum distinct hope-token hits on the teller text to earn <see cref="NeutralSpreadTellerBonus"/>.</summary>
    public const int NeutralSpreadHopeHitsRequired = 2;

    /// <summary>Slider at 100 → player wins regardless of computed totals.</summary>
    public static bool IsGuaranteedPlayerWin(float magicalEnergy0to100) =>
        magicalEnergy0to100 >= 99.5f;

    static readonly string[] GreedTokens =
    {
        "greed", "hunger", "feast", "feasting", "glutton", "gold", "coin", "coins", "wealth", "covet",
        "avarice", "devour", "banquet", "meal", "meals"
    };

    static readonly string[] VanityTokens =
    {
        "vanity", "mirror", "pride", "ego", "beauty", "appearance", "image", "vain", "looks", "surface",
        "reflection", "mask"
    };

    static readonly string[] ChaosTokens =
    {
        "chaos", "disorder", "storm", "wild", "spiral", "chance", "fray", "tumble", "mayhem", "wreck",
        "shatter", "snap"
    };

    static readonly string[] PowerTokens =
    {
        "power", "control", "throne", "crown", "rule", "dominion", "command", "chain", "weight",
        "heavy", "authority", "grip", "reign", "burden", "decree", "yoke", "rank"
    };

    static readonly string[] HopeHelpTokens =
    {
        "hope", "prosper", "peace", "heal", "light", "bless", "blessing", "kindness", "better", "grow",
        "strength", "courage", "guide", "balance", "agency", "walk lightly", "navigate", "uplift", "safe",
        "steady", "renew", "care", "warmth", "together", "support", "free", "freed", "fortune", "forgive",
        "forgiveness", "release", "relief"
    };

    static readonly string[] HarmDoomTokens =
    {
        "ruin", "doom", "curse", "decay", "rot", "wither", "shame", "dread", "corrupt", "hollow", "trap",
        "fall", "die", "death", "devour", "consume", "desolate", "shatter", "bleak", "hunger for", "bones",
        "darkness", "succumb", "excess", "corruption"
    };

    static readonly HashSet<string> DescriptionStopWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "like", "the", "a", "an", "with", "for", "into", "after", "you", "your", "act", "that", "this", "from",
        "and", "or", "but", "in", "on", "at", "to", "of", "is", "are", "was", "were", "be", "has", "have", "had",
        "do", "does", "did", "will", "would", "could", "should", "can", "may", "might", "must", "not", "no", "so",
        "if", "as", "it", "its", "one", "two", "three", "all", "any", "each", "every", "some", "such", "than",
        "then", "when", "where", "who", "what", "how", "why", "about", "upon", "over", "under", "out", "up", "down",
        "piece", "very", "just", "only", "also", "still", "even", "much", "many", "more", "most", "less", "least"
    };

    /// <summary>Computes both sides' totals from spread data and the two readings. Moral card tilt uses printed leans; theme/alignment use reading text (include spirit's full reply, e.g. overall moral read sentence).</summary>
    /// <param name="magicalEnergy0to100">0–100 from UI; adds up to <see cref="MagicalEnergyMaxBonusPoints"/> to the player; 100 = guaranteed win (see <see cref="IsGuaranteedPlayerWin"/>).</param>
    /// <param name="clientWealth">Wealth of the seated customer — wealth-fit tokens scored on both readings.</param>
    /// <param name="playerExperienceJudgeBias">Lineage difficulty bonus on the teller's total (see <see cref="RunExperienceConfig"/>).</param>
    public static FortuneDuelScoreBreakdown Compute(
        IReadOnlyList<TarotCardData> spread,
        string playerReading,
        string demonReading,
        float magicalEnergy0to100,
        FortuneClientSpawner.WealthType clientWealth = FortuneClientSpawner.WealthType.Poor,
        int playerExperienceJudgeBias = 0)
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
        int pDesc = 0;
        int dDesc = 0;

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
            pDesc += DescriptionEchoForReading(pNorm, c);
            dDesc += DescriptionEchoForReading(dNorm, c);
        }

        int pAlign = AlignmentForPlayer(pNorm);
        int dAlign = AlignmentForDemon(dNorm);
        int pWealth = FortuneClientWealthContext.ScoreWealthFitForPlayer(pNorm, clientWealth);
        int dWealth = FortuneClientWealthContext.ScoreWealthFitForDemon(dNorm, clientWealth);

        MoralSpreadJudgeBias(spread, out int pJudgeBias, out int dJudgeBias);

        int neutralBonus = ComputeNeutralSpreadTellerBonus(spread, pNorm);
        int lineageBias = Mathf.Max(0, playerExperienceJudgeBias);
        int pTotal = magic + pMoral + pTheme + pDesc + pWealth + pAlign + pJudgeBias + neutralBonus + lineageBias;
        int dTotal = dMoral + dTheme + dDesc + dWealth + dAlign + dJudgeBias;

        return new FortuneDuelScoreBreakdown(
            magic, pMoral, dMoral, pTheme, dTheme, pDesc, dDesc, pWealth, dWealth,
            pAlign, dAlign, pJudgeBias, dJudgeBias, neutralBonus, lineageBias, pTotal, dTotal, clientWealth);
    }

    /// <summary>All drawn lines Neutral + teller shows real hope → small booth bonus so neutral spreads are not pure token DPS races.</summary>
    static int ComputeNeutralSpreadTellerBonus(IReadOnlyList<TarotCardData> spread, string playerCorpusNorm)
    {
        if (spread == null || spread.Count == 0)
            return 0;
        for (int i = 0; i < spread.Count; i++)
        {
            if (spread[i].cardMoral != TarotMoral.Neutral)
                return 0;
        }

        if (CountBoundaryHits(playerCorpusNorm, HopeHelpTokens) < NeutralSpreadHopeHitsRequired)
            return 0;
        return NeutralSpreadTellerBonus;
    }

    /// <summary>More Good than Bad cards on the spread favors the teller; more Bad than Good favors the spirit; ties or neutral-only give no bias.</summary>
    static void MoralSpreadJudgeBias(IReadOnlyList<TarotCardData> spread, out int playerBonus, out int demonBonus)
    {
        playerBonus = 0;
        demonBonus = 0;
        if (spread == null)
            return;

        int good = 0, bad = 0;
        for (int i = 0; i < spread.Count; i++)
        {
            switch (spread[i].cardMoral)
            {
                case TarotMoral.Good:
                    good++;
                    break;
                case TarotMoral.Bad:
                    bad++;
                    break;
            }
        }

        if (good > bad)
            playerBonus = MoralSpreadJudgeBiasPoints;
        else if (bad > good)
            demonBonus = MoralSpreadJudgeBiasPoints;
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
        sb.AppendLine("Morality layers — (1) **Card values:** each line's printed moral lean (Good / Neutral / Bad) feeds the tilt above and the spread judge bias below; those counts are fixed from data.");
        sb.AppendLine("(2) **Interpretation:** the teller and spirit passages are scored separately for theme weave, **loose vignette echo** (keywords from each card's description), **customer wealth fit**, and hope-vs-harm alignment from their wording. The spirit's closing **overall moral read** of the draw (prose) is **additive** color for the booth judge: it must not replace or override the printed leans, but helps read intent alongside them.");
        sb.Append("- Customer wealth for this round: **").Append(FortuneClientWealthContext.LabelFor(s.ClientWealth)).AppendLine("**.");
        if (s.PlayerSpreadMoralJudgeBias > 0)
            sb.AppendLine($"Moral judge bias (more Good than Bad on the draw): +{s.PlayerSpreadMoralJudgeBias} to teller.");
        if (s.DemonSpreadMoralJudgeBias > 0)
            sb.AppendLine($"Moral judge bias (more Bad than Good on the draw): +{s.DemonSpreadMoralJudgeBias} to spirit.");
        if (s.PlayerNeutralSpreadBonus > 0)
            sb.AppendLine($"Neutral-draw booth steadiness (teller hope, all Neutral lines): +{s.PlayerNeutralSpreadBonus} to teller.");
        if (s.PlayerExperienceJudgeBias > 0)
            sb.AppendLine($"Lineage judge favor (opening difficulty): +{s.PlayerExperienceJudgeBias} to teller.");
        sb.AppendLine($"Theme identification (per card, max {MaxThemeIdentificationPerCard} each; abstract lane lexicon): player +{s.PlayerThemeIdentification}, spirit +{s.DemonThemeIdentification}.");
        sb.AppendLine($"Vignette echo (per card, max {MaxDescriptionEchoPerCard} each; loose keywords from card descriptions): player +{s.PlayerDescriptionEcho}, spirit +{s.DemonDescriptionEcho}.");
        sb.AppendLine($"Customer wealth fit (max {FortuneClientWealthContext.MaxWealthFit} each): player +{s.PlayerWealthFit}, spirit +{s.DemonWealthFit}.");
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
            return $"The teller leads the spirit by {diff} point{(diff == 1 ? "" : "s")}, weaving hope, the draw's vignettes, and the customer's station more tightly than the curse.";

        return $"The spirit leads the teller by {diff} point{(diff == 1 ? "" : "s")}, sinking dread, the drawn themes, and the customer's station deeper than the hopeful lines.";
    }

    static string NormalizeCorpus(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9\s-]", " ");
    }

    static int DescriptionEchoForReading(string corpusNorm, TarotCardData card)
    {
        if (string.IsNullOrEmpty(corpusNorm) || card == null)
            return 0;
        string[] tokens = EchoTokensFromDescription(card.GetCardDescription());
        if (tokens == null || tokens.Length == 0)
            return 0;
        int hits = CountBoundaryHits(corpusNorm, tokens);
        if (hits <= 0)
            return 0;
        if (hits == 1)
            return 2;
        if (hits == 2)
            return 4;
        return MaxDescriptionEchoPerCard;
    }

    static string[] EchoTokensFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return System.Array.Empty<string>();

        var list = new List<string>();
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        string norm = NormalizeCorpus(description);
        string[] parts = norm.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string w = parts[i].Trim('-');
            if (w.Length < 3 || DescriptionStopWords.Contains(w))
                continue;
            if (seen.Add(w))
                list.Add(w);
        }

        // Hyphenated compounds in the original (e.g. deep-fry) also contribute split parts.
        string[] rawWords = Regex.Split(description.ToLowerInvariant(), @"[^a-z0-9\-]+");
        for (int i = 0; i < rawWords.Length; i++)
        {
            string raw = rawWords[i];
            if (!raw.Contains("-"))
                continue;
            string[] halves = raw.Split('-');
            for (int h = 0; h < halves.Length; h++)
            {
                string half = halves[h].Trim();
                if (half.Length < 3 || DescriptionStopWords.Contains(half))
                    continue;
                if (seen.Add(half))
                    list.Add(half);
            }
        }

        return list.ToArray();
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
            return 3;
        if (hits == 2)
            return 5;
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

    /// <summary>True when the reading names at least one theme lexicon word for this card's lane.</summary>
    public static bool PlayerIdentifiedCardTheme(string playerReading, TarotCardData card)
    {
        if (card == null || string.IsNullOrWhiteSpace(playerReading))
            return false;
        return ThemeIdentificationForReading(NormalizeCorpus(playerReading), card.cardTheme) > 0;
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
    public readonly int PlayerDescriptionEcho;
    public readonly int DemonDescriptionEcho;
    public readonly int PlayerWealthFit;
    public readonly int DemonWealthFit;
    public readonly int PlayerAlignment;
    public readonly int DemonAlignment;
    public readonly int PlayerSpreadMoralJudgeBias;
    public readonly int DemonSpreadMoralJudgeBias;
    /// <summary>Included in <see cref="PlayerTotal"/>; from all-Neutral spread + hope hits on teller text.</summary>
    public readonly int PlayerNeutralSpreadBonus;
    /// <summary>Included in <see cref="PlayerTotal"/>; from run lineage / difficulty selection.</summary>
    public readonly int PlayerExperienceJudgeBias;
    public readonly int PlayerTotal;
    public readonly int DemonTotal;
    public readonly FortuneClientSpawner.WealthType ClientWealth;

    public FortuneDuelScoreBreakdown(
        int playerMagicPower,
        int playerMoralFromCards,
        int demonMoralFromCards,
        int playerThemeIdentification,
        int demonThemeIdentification,
        int playerDescriptionEcho,
        int demonDescriptionEcho,
        int playerWealthFit,
        int demonWealthFit,
        int playerAlignment,
        int demonAlignment,
        int playerSpreadMoralJudgeBias,
        int demonSpreadMoralJudgeBias,
        int playerNeutralSpreadBonus,
        int playerExperienceJudgeBias,
        int playerTotal,
        int demonTotal,
        FortuneClientSpawner.WealthType clientWealth)
    {
        PlayerMagicPower = playerMagicPower;
        PlayerMoralFromCards = playerMoralFromCards;
        DemonMoralFromCards = demonMoralFromCards;
        PlayerThemeIdentification = playerThemeIdentification;
        DemonThemeIdentification = demonThemeIdentification;
        PlayerDescriptionEcho = playerDescriptionEcho;
        DemonDescriptionEcho = demonDescriptionEcho;
        PlayerWealthFit = playerWealthFit;
        DemonWealthFit = demonWealthFit;
        PlayerAlignment = playerAlignment;
        DemonAlignment = demonAlignment;
        PlayerSpreadMoralJudgeBias = playerSpreadMoralJudgeBias;
        DemonSpreadMoralJudgeBias = demonSpreadMoralJudgeBias;
        PlayerNeutralSpreadBonus = playerNeutralSpreadBonus;
        PlayerExperienceJudgeBias = playerExperienceJudgeBias;
        PlayerTotal = playerTotal;
        DemonTotal = demonTotal;
        ClientWealth = clientWealth;
    }
}
