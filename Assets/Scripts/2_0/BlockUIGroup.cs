using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BlockUIGroup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image mainIcon;
    public Transform variantContainer;
    public GameObject blockItemPrefab;

    private List<BlockUIItem> spawnedItems = new List<BlockUIItem>();
    private Coroutine closeRoutine;
    private RectTransform popupRect;
    private RectTransform buttonRect;

    private void Awake()
    {
        if (variantContainer != null) popupRect = variantContainer.GetComponent<RectTransform>();
        buttonRect = GetComponent<RectTransform>();
    }

    public void InitializeGroup(BlockInventoryRow rowData)
    {
        variantContainer.gameObject.SetActive(false);

        foreach (Transform child in variantContainer) Destroy(child.gameObject);
        spawnedItems.Clear();

        bool firstValidFound = false;

        foreach (var entry in rowData.columns)
        {
            if (entry == null || entry.Block == null) continue;

            GameObject newItemObj = Instantiate(blockItemPrefab, variantContainer);
            BlockUIItem uiItem = newItemObj.GetComponent<BlockUIItem>();
            uiItem.Setup(entry.Block);
            spawnedItems.Add(uiItem);

            if (!firstValidFound)
            {
                mainIcon.sprite = entry.Block.uiSprite;
                firstValidFound = true;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (closeRoutine != null) StopCoroutine(closeRoutine);

        variantContainer.gameObject.SetActive(true);

        foreach (var item in spawnedItems) item.UpdateCount();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (closeRoutine != null) StopCoroutine(closeRoutine);
        closeRoutine = StartCoroutine(CheckKeepOpen());
    }

    private IEnumerator CheckKeepOpen()
    {
        yield return new WaitForSeconds(0.1f);

        while (IsMouseOverRect(popupRect) || IsMouseOverRect(buttonRect))
        {
            yield return null;
        }

        variantContainer.gameObject.SetActive(false);
    }

    private bool IsMouseOverRect(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);
    }
}