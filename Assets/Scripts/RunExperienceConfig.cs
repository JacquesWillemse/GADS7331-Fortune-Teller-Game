using System;
using UnityEngine;

/// <summary>
/// Run-wide difficulty from the opening lineage question (1 = novice … 3 = veteran).
/// Drives judge bias, starting energy, book unlocks, and spirit card lore.
/// </summary>
public static class RunExperienceConfig
{
    public enum FortuneLineage
    {
        Novice = 1,
        Familiar = 2,
        Veteran = 3
    }

    public enum SpiritCardKnowledge
    {
        /// <summary>Spirit sees vignettes only — themes and morals hidden.</summary>
        VignettesOnly,
        /// <summary>Spirit knows themes; moral leans hidden.</summary>
        ThemesOnly,
        /// <summary>Spirit knows every theme and moral lean on the spread.</summary>
        Full
    }

    static FortuneLineage _lineage = FortuneLineage.Familiar;
    static bool _configured;

    public static bool IsConfigured => _configured;
    public static FortuneLineage Lineage => _lineage;

    public static event Action OnConfigured;

    public static void Configure(FortuneLineage lineage)
    {
        _lineage = lineage;
        _configured = true;
        OnConfigured?.Invoke();
    }

#if UNITY_EDITOR
    public static void ResetForEditor()
    {
        _configured = false;
        _lineage = FortuneLineage.Familiar;
    }
#endif

    public static int PlayerJudgeBiasPoints => _lineage switch
    {
        FortuneLineage.Novice => 12,
        FortuneLineage.Familiar => 6,
        _ => 0
    };

    public static int StartingEnergy => _lineage switch
    {
        FortuneLineage.Novice => 100,
        FortuneLineage.Familiar => 80,
        _ => 60
    };

    public static int StartingBookUnlocks => _lineage switch
    {
        FortuneLineage.Novice => 8,
        FortuneLineage.Familiar => 4,
        _ => 0
    };

    public static SpiritCardKnowledge SpiritKnowledge => _lineage switch
    {
        FortuneLineage.Novice => SpiritCardKnowledge.VignettesOnly,
        FortuneLineage.Familiar => SpiritCardKnowledge.ThemesOnly,
        _ => SpiritCardKnowledge.Full
    };

    public static string LineageTitle(FortuneLineage lineage) => lineage switch
    {
        FortuneLineage.Novice => "First Night Under the Tent",
        FortuneLineage.Familiar => "Apprenticed Once",
        FortuneLineage.Veteran => "Tenth-Generation Blood",
        _ => "Unknown"
    };

    public static string LineageBlurb(FortuneLineage lineage) => lineage switch
    {
        FortuneLineage.Novice =>
            "The spirits smell inexperience. The booth favors you, the Book opens wide, and the demon reads the pasteboards through a fog.",
        FortuneLineage.Familiar =>
            "Half-remembered rituals. A fair bargain: some lore, some favor, and a spirit that knows the themes but not every moral twist.",
        FortuneLineage.Veteran =>
            "Full carnival memory. No judge mercy, bare lore, thin energy — and the demon sees every theme and moral lean as clearly as you do.",
        _ => ""
    };

    public static string LineageStats(FortuneLineage lineage)
    {
        ConfigurePreview(lineage, out int bias, out int energy, out int unlocks, out SpiritCardKnowledge spirit);
        string spiritLabel = spirit switch
        {
            SpiritCardKnowledge.VignettesOnly => "spirit: vignettes only",
            SpiritCardKnowledge.ThemesOnly => "spirit: themes known",
            _ => "spirit: full lore"
        };
        return $"+{bias} judge favor · {energy} energy · {unlocks} book pages · {spiritLabel}";
    }

    static void ConfigurePreview(
        FortuneLineage lineage,
        out int bias,
        out int energy,
        out int unlocks,
        out SpiritCardKnowledge spirit)
    {
        bias = lineage switch
        {
            FortuneLineage.Novice => 12,
            FortuneLineage.Familiar => 6,
            _ => 0
        };
        energy = lineage switch
        {
            FortuneLineage.Novice => 100,
            FortuneLineage.Familiar => 80,
            _ => 60
        };
        unlocks = lineage switch
        {
            FortuneLineage.Novice => 8,
            FortuneLineage.Familiar => 4,
            _ => 0
        };
        spirit = lineage switch
        {
            FortuneLineage.Novice => SpiritCardKnowledge.VignettesOnly,
            FortuneLineage.Familiar => SpiritCardKnowledge.ThemesOnly,
            _ => SpiritCardKnowledge.Full
        };
    }
}
