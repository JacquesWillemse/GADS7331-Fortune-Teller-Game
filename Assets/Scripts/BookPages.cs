using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Pages Database", menuName = "Pages/Pages Database")]
public class BookPages : ScriptableObject
{
    public List<PageData> pages;
}

[System.Serializable]
public class PageData
{
    public string pageHeading;
    public string pageWriting;
    public Image pageImage;
}
