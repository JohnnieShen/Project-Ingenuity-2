using UnityEngine;

public class BlockUIMenu : MonoBehaviour
{
    [Header("References")]
    public Transform scrollContentRoot;
    public GameObject blockGroupPrefab;

    private void Start()
    {
        if (BlockInventoryManager.instance != null)
        {
            GenerateMenu();
        }
    }

    public void GenerateMenu()
    {
        foreach (Transform child in scrollContentRoot) Destroy(child.gameObject);

        BlockInventoryMatrix matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null) return;

        for (int r = 0; r < matrix.rowsCount; r++)
        {
            var row = matrix.rows[r];

            bool hasBlocks = false;
            foreach (var col in row.columns)
            {
                if (col != null && col.Block != null) hasBlocks = true;
            }

            if (!hasBlocks) continue;

            GameObject groupObj = Instantiate(blockGroupPrefab, scrollContentRoot);
            BlockUIGroup groupUI = groupObj.GetComponent<BlockUIGroup>();

            groupUI.InitializeGroup(row);
        }
    }
}