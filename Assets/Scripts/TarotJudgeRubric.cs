/// <summary>
/// Point rubric for the judge (fortune teller vs demon). Totals are out of 100 for the player
/// (90 from four dimensions + up to 10 energy) and 90 for the demon (no energy).
/// </summary>
public static class TarotJudgeRubric
{
    public const int MaxThemeAlignment = 25;
    public const int MaxMoralityRoleFit = 25;
    public const int MaxPersuasiveness = 25;
    public const int MaxRoleFidelity = 15;
    public const int MaxEnergyBonus = 10;

    /// <summary>Design notes for prompts / UI (not enforced in code beyond sums).</summary>
    public const string RubricSummary =
        "Theme alignment (0-25): how well each reading uses the spread's symbolic tensions without contradicting the draw. " +
        "Morality-role fit (0-25): fortune teller must align hopeful reframing with Good/Neutral/Bad card leans; demon must twist or deepen harm in line with those leans. " +
        "Persuasiveness (0-25): clarity, voice, and conviction within role. " +
        "Role fidelity (0-15): fortune teller = help/relief/agency for the querent only; demon = harm/corruption/dread only - never swap. " +
        "Energy bonus (0-10, player only): flat points the game adds for spent magic energy before comparing totals.";
}
