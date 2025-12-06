using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlockUIItem : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI countText;
    public Button selectButton;

    private Block _block;

    public void Setup(Block block)
    {
        _block = block;

        if (_block != null)
        {
            iconImage.sprite = _block.uiSprite;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = Color.clear;
        }

        UpdateCount();

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnItemClicked);
    }

    public void UpdateCount()
    {
        if (_block == null) return;

        int count = BlockInventoryManager.instance.GetBlockCount(_block);
        countText.text = count.ToString();
    }

    private void OnItemClicked()
    {
        if (BuildSystem.instance != null)
        {
            BuildSystem.instance.SelectBlock(_block);
            Debug.Log($"Selected Block via UI: {_block.BlockName}");
        }
        else
        {
            Debug.LogError("BuildSystem instance not found!");
        }
    }
}