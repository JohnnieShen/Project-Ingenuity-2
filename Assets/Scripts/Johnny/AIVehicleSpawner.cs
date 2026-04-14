using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AIVehicleSpawner : MonoBehaviour
{
    public static AIVehicleSpawner instance;

    [Header("Dependencies")]
    public GameObject enemyCorePrefab; 
    public BlockInventoryMatrix inventoryMatrix; // Same matrix used in your save system

    private Dictionary<string, Block> blockRegistry;
    private int enemyBlockLayer;
    private int shieldLayerIndex;
    private string aiSaveDirectory;

    private void Awake()
    {
        if (instance == null) instance = this;

        aiSaveDirectory = Path.Combine(Application.persistentDataPath, "AIVehicles");
        if (!Directory.Exists(aiSaveDirectory))
        {
            Directory.CreateDirectory(aiSaveDirectory);
        }

        enemyBlockLayer = LayerMask.NameToLayer("EnemyBlock");
        shieldLayerIndex = LayerMask.NameToLayer("Shield");

        // Build the dictionary for fast lookups
        blockRegistry = new Dictionary<string, Block>();
        if (inventoryMatrix != null && inventoryMatrix.rows != null)
        {
            foreach (var row in inventoryMatrix.rows)
            {
                if (row == null || row.columns == null) continue;
                foreach (var template in row.columns)
                {
                    if (template != null && template.Block != null && !blockRegistry.ContainsKey(template.Block.BlockName))
                    {
                        blockRegistry.Add(template.Block.BlockName, template.Block);
                    }
                }
            }
        }
    }

    public GameObject SpawnAIVehicle(string jsonFileName, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        string path = Path.Combine(aiSaveDirectory, jsonFileName + ".json");
        
        if (!File.Exists(path))
        {
            Debug.LogError("AI Save file not found at: " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        VehicleSaveData saveData = JsonUtility.FromJson<VehicleSaveData>(json);

        // 1. Spawn the stationary root container
        GameObject aiRootObj = Instantiate(enemyCorePrefab, spawnPosition, spawnRotation);
        
        // 2. Find the actual moving core inside the root
        Transform cmdModuleTransform = aiRootObj.transform.Find("CommandModule");
        if (cmdModuleTransform == null)
        {
            Debug.LogError("Could not find a child named 'CommandModule' on the AI prefab!");
            return null;
        }

        EnemyAI enemyAI = cmdModuleTransform.GetComponent<EnemyAI>();
        Rigidbody coreRb = cmdModuleTransform.GetComponent<Rigidbody>();

        // Ensure the core is registered in the EnemyBlockManager at (0,0,0)
        EnemyBlockManager.instance.RegisterBlock(enemyAI, Vector3Int.zero, coreRb);

        List<(GameObject blockObj, Vector3Int gridPos, Hull hull)> loadedBlocks = new List<(GameObject, Vector3Int, Hull)>();

        // ==========================================
        // PHASE 1: Instantiate & Convert to AI
        // ==========================================
        foreach (BlockSaveData blockData in saveData.blocks)
        {
            if (blockRegistry.TryGetValue(blockData.blockName, out Block sourceBlock))
            {
                // Instantiate as a child of the stationary root
                GameObject newBlock = Instantiate(sourceBlock.BlockObject, aiRootObj.transform);
                
                // IMPORTANT: Reconstruct positions using the moving CommandModule, not the root
                newBlock.transform.position = cmdModuleTransform.TransformPoint(blockData.localPosition);
                newBlock.transform.rotation = cmdModuleTransform.rotation * blockData.localRotation;

                // Strip player-only scripts
                Destroy(newBlock.GetComponent<AimSphere>());
                Destroy(newBlock.GetComponent<VehicleResourceManager>());

                // Apply AI Conversions
                newBlock.tag = "EnemyBlock";
                
                Wheel wheel = newBlock.GetComponentInChildren<Wheel>();
                if (wheel != null) wheel.isAI = true;

                Turret turret = newBlock.GetComponent<Turret>();
                if (turret != null) turret.isAI = true;

                ShieldGenerator shield = newBlock.GetComponent<ShieldGenerator>();
                if (shield != null) shield.SetAI(true);

                foreach (Transform t in newBlock.GetComponentsInChildren<Transform>(true))
                {
                    if (t.gameObject.layer != shieldLayerIndex) 
                        t.gameObject.layer = enemyBlockLayer;
                }

                foreach (Transform t in newBlock.GetComponentsInChildren<Transform>(true))
                {
                    if (t.CompareTag("BuildVisualWidget")) t.gameObject.SetActive(false);
                }

                Hull newHull = newBlock.GetComponent<Hull>();
                if (newHull != null) newHull.sourceBlock = sourceBlock;

                Rigidbody rb = newBlock.GetComponent<Rigidbody>();
                Vector3Int gridPos = Vector3Int.RoundToInt(blockData.localPosition);

                if (EnemyBlockManager.instance != null && rb != null)
                {
                    EnemyBlockManager.instance.RegisterBlock(enemyAI, gridPos, rb);
                }

                loadedBlocks.Add((newBlock, gridPos, newHull));
            }
        }

        // ==========================================
        // PHASE 2: Weld Joints
        // ==========================================
        foreach (var item in loadedBlocks)
        {
            if (item.hull == null || item.hull.validConnectionOffsets == null) continue;

            foreach (Vector3Int offset in item.hull.validConnectionOffsets)
            {
                Vector3 offsetLocal = offset;
                Vector3 offsetWorld = item.blockObj.transform.TransformDirection(offsetLocal);
                
                // IMPORTANT: Calculate grid offsets using the CommandModule's rotation
                Vector3 offsetInModule = cmdModuleTransform.InverseTransformDirection(offsetWorld);
                
                Vector3Int offsetInModuleInt = new Vector3Int(
                    Mathf.RoundToInt(offsetInModule.x),
                    Mathf.RoundToInt(offsetInModule.y),
                    Mathf.RoundToInt(offsetInModule.z)
                );
                
                Vector3Int neighborPos = item.gridPos + offsetInModuleInt;

                if (EnemyBlockManager.instance.TryGetBlock(enemyAI, neighborPos, out Rigidbody neighborRb))
                {
                    if (AlreadyConnected(item.blockObj, neighborRb)) continue;

                    Hull neighborHull = neighborRb.GetComponent<Hull>();
                    if (neighborHull != null || neighborPos == Vector3Int.zero) 
                    {
                        var joint = item.blockObj.AddComponent<FixedJoint>();
                        joint.connectedBody = neighborRb;
                        
                        float myStrength = item.hull.sourceBlock != null ? item.hull.sourceBlock.connectionStrength : 1000f;
                        float neighborStrength = (neighborHull != null && neighborHull.sourceBlock != null) ? neighborHull.sourceBlock.connectionStrength : 1000f;
                        
                        joint.breakForce = myStrength + neighborStrength;
                    }
                }
            }
        }

        // 3. Finalize the AI Setup
        enemyAI.InitializeVehicleStructure(); 

        Rigidbody[] allRigidbodies = aiRootObj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rbody in allRigidbodies)
        {
            rbody.isKinematic = false;
            rbody.WakeUp();
        }

        return aiRootObj;
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
}