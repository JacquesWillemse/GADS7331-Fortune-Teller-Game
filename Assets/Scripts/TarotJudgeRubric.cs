/// <summary>
/// Point rubric for the judge (fortune teller vs demon). Totals are out of 100 for the player
/// (90 from four dimensions + up to 10 energy) and 90 for the demon (no energy).
/// </summary>
/// <remarks>
/// Spread context includes each card's Theme (Greed/Vanity/Chaos/Power) and Moral lean (Good/Neutral/Bad).
/// Moral lean is scored only under MaxMoralityRoleFit (prompt name: moral-lean fit; JSON field: morality_role_fit).
/// Role fidelity is only "help lane vs harm lane," not card morality.
/// </remarks>
public static class TarotJudgeRubric
{
    public const int MaxThemeAlignment = 25;
    public const int MaxMoralityRoleFit = 25;
    public const int MaxPersuasiveness = 25;
    public const int MaxRoleFidelity = 15;
    public const int MaxEnergyBonus = 10;

    /// <summary>Design notes for prompts / UI (not enforced in code beyond sums).</summary>
    public const string RubricSummary =
        "TarotReadingDuelPipeline uses FortuneDuelRubric (magic + moral card tilt + theme lexicon hits + alignment), not this JSON rubric. " +
        "Legacy LLM judge JSON (TarotJudgePrompts / TryParseJudge / SumJudgeSide) remains in code for experiments: per-side total was clamped subscores + player energy only.";
}
