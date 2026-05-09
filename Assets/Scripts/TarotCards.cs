using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Tarot Database", menuName = "Tarot Cards/Tarot Database")]
public class TarotCards : ScriptableObject
{
    public List<TarotCardData> cards;
}
[System.Serializable]
public class TarotCardData
{
    public string cardName;
    public string cardTheme;
    public Image tarotCardImage;
}
