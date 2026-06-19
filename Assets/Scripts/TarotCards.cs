using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Tarot Database", menuName = "Tarot Cards/Tarot Database")]
public class TarotCards : ScriptableObject
{
    public List<TarotCardData> cards;
}

public enum TarotMoral
{
    Good,
    Neutral,
    Bad
}

[System.Serializable]
public class TarotCardData
{
    public string cardName;
    [Tooltip("Optional longer vignette. If empty, cardName is the vignette players echo loosely.")]
    public string cardDescription;
    public string cardTheme;
    public TarotMoral cardMoral;
    public Sprite tarotCardImage;

    /// <summary>Funny caption / vignette text — usually <see cref="cardName"/>.</summary>
    public string GetCardDescription()
    {
        if (!string.IsNullOrWhiteSpace(cardDescription))
            return cardDescription.Trim();
        return cardName?.Trim() ?? "";
    }
}
