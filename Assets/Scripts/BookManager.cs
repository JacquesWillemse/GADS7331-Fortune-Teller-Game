using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookManager : MonoBehaviour
{
    [SerializeField] private BookPages bookPages;

    [SerializeField] private TMP_Text leftPageTitle;
    [SerializeField] private TMP_Text leftPageBody;
    [SerializeField] private TMP_Text rightPageTitle;
    [SerializeField] private TMP_Text rightPageBody;

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
        ApplyPage(leftPageTitle, leftPageBody, list, _spreadStartIndex);
        ApplyPage(rightPageTitle, rightPageBody, list, _spreadStartIndex + 1);
        UpdateNavButtons(list);
    }

    static void ApplyPage(TMP_Text titleTmp, TMP_Text bodyTmp, List<PageData> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
            ClearTmp(titleTmp);
            ClearTmp(bodyTmp);
            return;
        }

        PageData page = list[index];
        if (page == null)
        {
            ClearTmp(titleTmp);
            ClearTmp(bodyTmp);
            return;
        }

        SetTmp(titleTmp, page.pageHeading);
        SetTmp(bodyTmp, page.pageWriting);
    }

    static void SetTmp(TMP_Text tmp, string value)
    {
        if (tmp == null)
            return;
        tmp.text = value ?? "";
    }

    static void ClearTmp(TMP_Text tmp)
    {
        if (tmp == null)
            return;
        tmp.text = "";
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
