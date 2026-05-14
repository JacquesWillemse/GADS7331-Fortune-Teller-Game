using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookManager : MonoBehaviour
{
    [SerializeField] private BookPages bookPages;
    [SerializeField] private TMP_Text leftPage;
    [SerializeField] private TMP_Text rightPage;

    [SerializeField] private Button previousPage;
    [SerializeField] private Button nextPage;

    int _spreadStartIndex;

    void Start()
    {
        if (nextPage != null)
            nextPage.onClick.AddListener(OnNextPage);
        if (previousPage != null)
            previousPage.onClick.AddListener(OnPreviousPage);
        _spreadStartIndex = 0;
        RefreshView();
    }

    void OnDestroy()
    {
        if (nextPage != null)
            nextPage.onClick.RemoveListener(OnNextPage);
        if (previousPage != null)
            previousPage.onClick.RemoveListener(OnPreviousPage);
    }

    void OnNextPage()
    {
        List<PageData> list = bookPages != null ? bookPages.pages : null;
        if (list == null || list.Count == 0)
            return;
        if (_spreadStartIndex + 2 < list.Count)
        {
            _spreadStartIndex += 2;
            RefreshView();
        }
    }

    void OnPreviousPage()
    {
        if (_spreadStartIndex >= 2)
        {
            _spreadStartIndex -= 2;
            RefreshView();
        }
    }

    void RefreshView()
    {
        List<PageData> list = bookPages != null ? bookPages.pages : null;
        ApplyPage(leftPage, list, _spreadStartIndex);
        ApplyPage(rightPage, list, _spreadStartIndex + 1);
        UpdateNavButtons(list);
    }

    static void ApplyPage(TMP_Text tmp, List<PageData> list, int index)
    {
        if (tmp == null)
            return;
        if (list == null || index < 0 || index >= list.Count)
        {
            tmp.text = "";
            return;
        }

        PageData page = list[index];
        if (page == null)
        {
            tmp.text = "";
            return;
        }

        if (string.IsNullOrEmpty(page.pageHeading))
            tmp.text = page.pageWriting ?? "";
        else if (string.IsNullOrEmpty(page.pageWriting))
            tmp.text = page.pageHeading;
        else
            tmp.text = page.pageHeading + "\n\n" + page.pageWriting;
    }

    void UpdateNavButtons(List<PageData> list)
    {
        bool hasPages = list != null && list.Count > 0;
        bool canPrev = hasPages && _spreadStartIndex >= 2;
        bool canNext = hasPages && _spreadStartIndex + 2 < list.Count;

        if (previousPage != null)
            previousPage.interactable = canPrev;
        if (nextPage != null)
            nextPage.interactable = canNext;
    }
}
