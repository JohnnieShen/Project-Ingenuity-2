using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VehicleSaveSystem : MonoBehaviour
{
    public static VehicleSaveSystem instance;

    [Header("References")]
    public Transform vehicleParent; 
    public BlockInventoryMatrix inventoryMatrix; // Replaced the List with your Matrix
    
    public Transform commandModule; 

    private Dictionary<string, Block> blockRegistry;

    private void Awake()
    {
        if (instance == null) instance = this;

        blockRegistry = new Dictionary<string, Block>();

        // Safely iterate through the 2D matrix to register all available blocks
        if (inventoryMatrix != null && inventoryMatrix.rows != null)
        {
            for (int r = 0; r < inventoryMatrix.rowsCount; r++)
            {
                var row = inventoryMatrix.rows[r];
                if (row == null || row.columns == null) continue;

                for (int c = 0; c < inventoryMatrix.columnsCount; c++)
                {
                    var template = row.columns[c];
                    
                    // Check if the slot actually contains a block
                    if (template != null && template.Block != null)
                    {
                        if (!blockRegistry.ContainsKey(template.Block.BlockName))
                        {
                            blockRegistry.Add(template.Block.BlockName, template.Block);
                        }
                    }
                }
            }
        }
    }

    public void SaveVehicle(string fileName)
    {
        VehicleSaveData saveData = new VehicleSaveData();
        saveData.vehicleName = fileName;

        Hull[] currentBlocks = vehicleParent.GetComponentsInChildren<Hull>();

        foreach (Hull hull in currentBlocks)
        {
            if (hull.gameObject == commandModule.gameObject) continue;
            if (hull.sourceBlock == null) continue;

            BlockSaveData blockData = new BlockSaveData
            {
                blockName = hull.sourceBlock.BlockName,
                // IMPORTANT: Save coordinates relative to the Command Module, not the parent
                localPosition = commandModule.InverseTransformPoint(hull.transform.position),
                localRotation = Quaternion.Inverse(commandModule.rotation) * hull.transform.rotation
            };

            saveData.blocks.Add(blockData);
        }

        string json = JsonUtility.ToJson(saveData, true);
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        File.WriteAllText(path, json);
        
        Debug.Log($"Vehicle saved successfully to: {path}");
    }

    public void LoadVehicle(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("Save file not found at: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        VehicleSaveData saveData = JsonUtility.FromJson<VehicleSaveData>(json);

        // Freeze physics to prevent blocks from exploding apart upon instantiation
        if (BlockManager.instance != null) BlockManager.instance.DisableVehiclePhysics();

        ClearCurrentVehicle();

        List<(GameObject blockObj, Vector3Int gridPos, Hull hull)> loadedBlocks = new List<(GameObject, Vector3Int, Hull)>();

        foreach (BlockSaveData blockData in saveData.blocks)
        {
            if (blockRegistry.TryGetValue(blockData.blockName, out Block sourceBlock))
            {
                GameObject newBlock = Instantiate(sourceBlock.BlockObject, vehicleParent);
                
                // IMPORTANT: Reconstruct World Space coordinates based on the Command Module's current location
                newBlock.transform.position = commandModule.TransformPoint(blockData.localPosition);
                newBlock.transform.rotation = commandModule.rotation * blockData.localRotation;

                Hull newHull = newBlock.GetComponent<Hull>();
                if (newHull != null)
                {
                    newHull.sourceBlock = sourceBlock;
                }

                Rigidbody rb = newBlock.GetComponent<Rigidbody>();
                
                Vector3Int gridPos = Vector3Int.RoundToInt(blockData.localPosition);

                if (BlockManager.instance != null && rb != null)
                {
                    BlockManager.instance.AddBlock(gridPos, rb);
                }

                loadedBlocks.Add((newBlock, gridPos, newHull));
            }
            else
            {
                Debug.LogWarning($"Block '{blockData.blockName}' not found in registry.");
            }
        }

        Transform referenceTransform = commandModule;

        foreach (var item in loadedBlocks)
        {
            GameObject newBlock = item.blockObj;
            Vector3Int spawnPosInt = item.gridPos;
            Hull newHull = item.hull;

            if (newHull == null || newHull.validConnectionOffsets == null || referenceTransform == null) continue;

            foreach (Vector3Int offset in newHull.validConnectionOffsets)
            {
                Vector3 offsetLocal = offset;
                Vector3 offsetWorld = newBlock.transform.TransformDirection(offsetLocal);
                Vector3 offsetInModule = referenceTransform.InverseTransformDirection(offsetWorld);
                
                Vector3Int offsetInModuleInt = new Vector3Int(
                    Mathf.RoundToInt(offsetInModule.x),
                    Mathf.RoundToInt(offsetInModule.y),
                    Mathf.RoundToInt(offsetInModule.z)
                );
                
                Vector3Int neighborPos = spawnPosInt + offsetInModuleInt;

                if (BlockManager.instance != null && BlockManager.instance.TryGetBlockAt(neighborPos, out Rigidbody neighborRb))
                {
                    if (AlreadyConnected(newBlock, neighborRb)) continue;

                    Hull neighborHull = neighborRb.GetComponent<Hull>();
                    if (neighborHull != null)
                    {
                        Vector3Int gridOffset = spawnPosInt - neighborPos;
                        Vector3 gridOffsetVec = (Vector3)gridOffset;

                        Quaternion neighborGridRot = Quaternion.Inverse(referenceTransform.rotation) * neighborRb.transform.rotation;
                        Vector3 neighborLocalOffset = Quaternion.Inverse(neighborGridRot) * gridOffsetVec;
                        
                        Vector3Int neighborLocalOffsetInt = new Vector3Int(
                            Mathf.RoundToInt(neighborLocalOffset.x),
                            Mathf.RoundToInt(neighborLocalOffset.y),
                            Mathf.RoundToInt(neighborLocalOffset.z)
                        );

                        if (neighborHull.validConnectionOffsets.Contains(neighborLocalOffsetInt))
                        {
                            var joint = newBlock.AddComponent<FixedJoint>();
                            joint.connectedBody = neighborRb;
                            joint.breakForce = newHull.sourceBlock.connectionStrength + neighborHull.sourceBlock.connectionStrength;

                            BlockManager.instance.AddConnection(spawnPosInt, neighborPos);
                        }
                    }
                }
            }
        }

        if (BlockManager.instance != null && ModeSwitcher.instance != null)
        {
            if (ModeSwitcher.instance.currentMode == ModeSwitcher.Mode.Drive)
            {
                BlockManager.instance.EnableVehiclePhysics();
            }
            else
            {
                BlockManager.instance.DisableVehiclePhysics();
            }
        }

        Debug.Log("Vehicle loaded and welded successfully!");
    }

    private void ClearCurrentVehicle()
    {
        Hull[] currentBlocks = vehicleParent.GetComponentsInChildren<Hull>();
        foreach (Hull hull in currentBlocks)
        {
            if (hull.gameObject == commandModule.gameObject) continue;

            if (hull.sourceBlock != null) 
            {
                // IMPORTANT: Calculate the grid position to remove from BlockManager based on the Command Module
                Vector3 localPos = commandModule.InverseTransformPoint(hull.transform.position);
                Vector3Int localPosInt = Vector3Int.RoundToInt(localPos);

                if (BlockManager.instance != null)
                {
                    BlockManager.instance.RemoveBlock(localPosInt);
                }

                Destroy(hull.gameObject);
            }
        }
    }

    private bool AlreadyConnected(GameObject blockObj, Rigidbody neighborRb)
    {
        FixedJoint[] existingJoints = blockObj.GetComponents<FixedJoint>();
        foreach (var joint in existingJoints)
        {
            if (joint.connectedBody == neighborRb) return true;
        }
        return false;
    }

    public void DeleteVehicle(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Deleted save file: {path}");
        }
        else
        {
            Debug.LogWarning($"Cannot find save file to delete: {path}");
        }
    }
}