using System.Collections.Generic;
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
    public string cardTheme;
    public TarotMoral cardMoral;
    public Image tarotCardImage;
}
