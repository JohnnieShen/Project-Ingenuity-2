using UnityEngine;
using System;

[System.Serializable]
public class BlockInventoryRow
{
    public BlockInventory[] columns;
}

[CreateAssetMenu(
    fileName = "BlockInventoryMatrix",
    menuName = "Inventory/Block Inventory Matrix")]
public class BlockInventoryMatrix : ScriptableObject
{
    [Header("Matrix dimensions")]
    [Min(1)] public int rowsCount = 3;
    [Min(1)] public int columnsCount = 4;

    [Header("Block inventory rows x levels")]
    public BlockInventoryRow[] rows;

    private void OnValidate()
    {
        if (rowsCount < 1) rowsCount = 1;
        if (columnsCount < 1) columnsCount = 1;

        BlockInventoryRow[] oldRows = rows ?? Array.Empty<BlockInventoryRow>();
        int oldRowCount = oldRows.Length;

        BlockInventoryRow[] newRows = new BlockInventoryRow[rowsCount];

        for (int r = 0; r < rowsCount; r++)
        {
            BlockInventoryRow srcRow = (r < oldRowCount) ? oldRows[r] : null;
            if (srcRow == null)
                srcRow = new BlockInventoryRow();

            BlockInventory[] oldCols = srcRow.columns ?? Array.Empty<BlockInventory>();
            int oldColCount = oldCols.Length;

            BlockInventory[] newCols = new BlockInventory[columnsCount];

            int copyCols = Mathf.Min(columnsCount, oldColCount);
            for (int c = 0; c < copyCols; c++)
            {
                newCols[c] = oldCols[c];
            }

            srcRow.columns = newCols;
            newRows[r] = srcRow;
        }

        rows = newRows;
    }

    public BlockInventory GetEntry(int r, int c) => rows[r].columns[c];
    public void SetEntry(int r, int c, BlockInventory bi) => rows[r].columns[c] = bi;
}