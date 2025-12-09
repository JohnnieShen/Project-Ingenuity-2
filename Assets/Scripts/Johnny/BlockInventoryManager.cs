using System.Collections.Generic;
using UnityEngine;


/* BlockInventory tracks the inventory of blocks in the game.
 * It contains a Block reference, the current count of that block, and the maximum stack size.
 */
[System.Serializable]
public class BlockInventory
{
    public Block Block;
    public int CurrentCount;
    public int MaxStack = 99;
}

public class BlockInventoryManager : MonoBehaviour
{
    /* 
    * Author: Johnny
    * Summary: This script manages the inventory of blocks in the game. It allows for adding, consuming, and checking the count of blocks.
    * It also provides functionality to initialize the inventory and check if a block is present in the inventory.
    * The script uses a singleton pattern to ensure that only one instance of the BlockInventoryManager exists in the game.
    */

    public static BlockInventoryManager instance;

    [Header("Block Inventory Setup")]
    public BlockInventoryMatrix inventoryMatrix;
    private Dictionary<Block, BlockInventory> blockInventory = new Dictionary<Block, BlockInventory>();

    /* Awake is called when the script instance is being loaded.
    * It initializes the singleton instance and calls the InitializeInventory method to set up the block inventory.
    */
    private void Awake()
    {
        if (instance == null) instance = this;
        else Debug.LogError("Duplicated BlockInventoryManager!", gameObject);

        InitializeInventory();
    }

    /* InitializeInventory sets up the block inventory by clearing the existing inventory and populating it with available building blocks.
    * It iterates through the available building blocks and adds them to the block inventory dictionary.
    * If a block is null or has no Block reference, it is skipped.
    */
    private void InitializeInventory()
    {
        blockInventory.Clear();
        if (inventoryMatrix == null || inventoryMatrix.rows == null) return;

        for (int r = 0; r < inventoryMatrix.rowsCount; r++)
        {
            var row = inventoryMatrix.rows[r];
            if (row?.columns == null) continue;

            for (int c = 0; c < inventoryMatrix.columnsCount; c++)
            {
                var template = row.columns[c];
                if (template == null || template.Block == null)
                    continue;

                var runtimeEntry = new BlockInventory
                {
                    Block = template.Block,
                    CurrentCount = template.CurrentCount,
                    MaxStack = template.MaxStack
                };

                blockInventory.Add(runtimeEntry.Block, runtimeEntry);
            }
        }
    }

    /* It returns whether a block is present in the inventory.
    * Param 1: block - The block to check for in the inventory.
    * Returns true if the block is present, false otherwise.
    */
    public bool ContainsBlock(Block block)
    {
        return block != null && blockInventory.ContainsKey(block);
    }

    /* It returns the current count of a specific block in the inventory.
    * Param 1: block - The block to check the count for.
    * Returns the current count of the block in the inventory.
    */
    public int GetBlockCount(Block block)
    {
        if (block != null && blockInventory.TryGetValue(block, out var inventoryEntry))
        {
            return inventoryEntry.CurrentCount;
        }
        return 0;
    }

    /* Consumes a block if it is present in the inventory and the amount to consume is valid.
    * It checks if the block is null or if the amount is less than or equal to zero, and returns false if so.
    * If the block is present in the inventory and the current count is greater than or equal to the amount to consume,
    * it reduces the current count by the amount and returns true.
    * Otherwise, it returns false.
    * Param 1: block - The block to consume from the inventory.
    * Param 2: amount - The amount of the block to consume.
    */
    public bool TryConsumeBlock(Block block, int amount)
    {
        if (block == null || amount <= 0) return false;

        if (blockInventory.TryGetValue(block, out var entry))
        {
            if (entry.CurrentCount >= amount)
            {
                entry.CurrentCount -= amount;
                blockInventory[block] = entry;
                return true;
            }
        }
        return false;
    }

    /* It adds a block to the inventory if it is not null and the amount is valid.
    * It checks if the block is null or if the amount is less than or equal to zero, and returns if so.
    * If the block is already present in the inventory, it increases the current count by the amount,
    * ensuring that it does not exceed the maximum stack size.
    * If the block is not present, it creates a new BlockInventory entry for the block and adds it to the inventory.
    * Param 1: block - The block to add to the inventory.
    * Param 2: amount - The amount of the block to add to the inventory.
    */
    public void AddBlock(Block block, int amount)
    {
        if (block == null || amount <= 0) return;

        if (blockInventory.TryGetValue(block, out var entry))
        {
            // Debug.Log($"Adding {amount} of {block.BlockName} to inventory. Current: {entry.CurrentCount}, Max: {entry.MaxStack}");
            entry.CurrentCount = Mathf.Min(entry.CurrentCount + amount, entry.MaxStack);
            blockInventory[block] = entry; 
        }
        else
        {
            // Debug.Log($"Adding new block {block.BlockName} to inventory with amount {amount}.");
            BlockInventory newEntry = new BlockInventory()
            {
                Block = block,
                CurrentCount = amount,
                MaxStack = 99 // or a default
            };
            blockInventory.Add(block, newEntry);
        }
    }

    /* Updates display string for a block in the inventory.
    * Param 1: block - The block to get the display string for.
    * Returns the display string for the block, including its name, current count, and maximum stack size.
    */
    public string GetDisplayString(Block block)
    {
        if (block == null) return "None";
        if (blockInventory.TryGetValue(block, out var entry))
        {
            return $"{block.BlockName} ({entry.CurrentCount}/{entry.MaxStack})";
        }
        return block.BlockName + " (not in inventory)";
    }
}
