using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    /* 
    * Author: Johnny
    * Summary: This script manages the blocks in the game. It allows for adding, removing, and checking the connections between blocks.
    * It also provides functionality to validate the structure of the vehicle by checking if all blocks are connected to the core block.
    * The script uses a singleton pattern to ensure that only one instance of the BlockManager exists in the game.
    * It also provides functionality to enable or disable the physics of all blocks in the vehicle.
    * The script uses a dictionary to keep track of the blocks and their positions, as well as the connections between blocks.
    * It also provides a method to recalculate the connections between blocks and clean up any broken joints.
    * The script is attached to a GameObject in the scene and requires a reference to the grid origin for calculating local positions.
    */

    [Header("Inventory Data")]
    public BlockInventoryMatrix inventoryMatrix;

    public static BlockManager instance;
    public Dictionary<Vector3Int, Rigidbody> blocks = new Dictionary<Vector3Int, Rigidbody>(); // Directory for storing the blocks and their positions
    // ^^^ Key is the position of the block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // ^^^ Value is the Rigidbody component of the block.
    public Dictionary<Vector3Int, List<Vector3Int>> blockConnections = new Dictionary<Vector3Int, List<Vector3Int>>(); // Directory for storing the connections between blocks
    // ^^^ Key is the position of the block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // ^^^ Value is a list of Vector3Int positions of the connected blocks. Each entry in the list is a connection from the key block to the connected block.
    private bool isValidating = false;
    public Transform gridOrigin;
    private readonly Dictionary<Block, int> vehicleBlockCounts = new();

    /* Awake is called when the script instance is being loaded.
    It enforces the singleton pattern by checking if an instance already exists.
    If it does, it logs an error message and destroys the new instance.
    */
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Debug.LogError("Duplicated BlockManager", gameObject);
    }

    // Add a block to the blocks directory
    // This function is used in the build logic to add blocks to the vehicle.
    // Param 1: localPos - The local position of the block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // Param 2: blockRb - The Rigidbody component of the block.
    public void AddBlock(Vector3Int localPos, Rigidbody blockRb)
    {
        // foreach (var block in blocks)
        // {
        //     Debug.Log("Block at " + block.Key + " with Rigidbody " + block.Value);
        // }
        // Debug.Log("Adding block at " + localPos + " with Rigidbody " + blockRb);
        if (!blocks.ContainsKey(localPos))
        {
            // Debug.Log("Adding block at " + localPos + " with Rigidbody " + blockRb + " to blocks directory");
            blocks.Add(localPos, blockRb);
        }
        var hull = blockRb.GetComponent<Hull>();
        if (hull != null && hull.sourceBlock != null)
        {
            // Debug.Log("Adding block at " + localPos + " with Hull " + hull + " to arrays");
            vehicleBlockCounts.TryGetValue(hull.sourceBlock, out int n);
            vehicleBlockCounts[hull.sourceBlock] = n + 1;

            VehicleBuildEvents.RaiseBlockAdded(hull.sourceBlock, localPos);
        }
    }
    // Check if a block exists at a given position
    // This function is used in the build logic to check if a block exists at a given position.
    // Param 1: localPos - The local position of the block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // Param 2: blockRb - The Rigidbody component of the block.
    public bool TryGetBlockAt(Vector3Int localPos, out Rigidbody blockRb)
    {
        return blocks.TryGetValue(localPos, out blockRb);
    }

    // Remove a block from the blocks directory
    // This function is used in the build logic to remove blocks from the vehicle.
    // Param 1: localPos - The local position of the block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // Param 2: blockRb - The Rigidbody component of the block.
    public void RemoveBlock(Vector3Int localPos)
    {
        if (!blocks.TryGetValue(localPos, out var rb)) return;

        var hull = rb != null ? rb.GetComponent<Hull>() : null;
        if (hull != null && hull.sourceBlock != null)
        {
            if (vehicleBlockCounts.TryGetValue(hull.sourceBlock, out int n))
                vehicleBlockCounts[hull.sourceBlock] = Mathf.Max(0, n - 1);

            VehicleBuildEvents.RaiseBlockRemoved(hull.sourceBlock, localPos);
        }

        blocks.Remove(localPos);
        RemoveConnections(localPos);
    }

    // ^^^ These functions are mainly used in the build logic, but also used in e.g. the block destruction logic.

    // Add a connection between two blocks
    // This function is used in the build logic to add connections between blocks.
    // Param 1: position1 - The local position of the first block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // Param 2: position2 - The local position of the second block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    public void AddConnection(Vector3Int position1, Vector3Int position2)
    {
        AddConnectionDirection(position1, position2);
        AddConnectionDirection(position2, position1);
    }

    /* Add a connection direction between two blocks
    * Param 1: from - The local position of the first block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    * Param 2: to - The local position of the second block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    */
    private void AddConnectionDirection(Vector3Int from, Vector3Int to)
    {
        if (!blockConnections.ContainsKey(from))
            blockConnections[from] = new List<Vector3Int>();
            
        if (!blockConnections[from].Contains(to))
            blockConnections[from].Add(to);
    }

    // Remove a connection between two blocks
    // This function is used in the build logic to remove connections between blocks.
    // Param 1: position1 - The local position of the first block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // Param 2: position2 - The local position of the second block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    public void RemoveConnections(Vector3Int position)
    {
        if (blockConnections.ContainsKey(position))
        {
            foreach (var connection in blockConnections[position])
            {
                if (blockConnections.ContainsKey(connection))
                    blockConnections[connection].Remove(position);
            }
            blockConnections.Remove(position);
        }
    }

    /* Cleanup broken joints
    * This function is used to clean up any broken joints in the blocks directory.
    * It iterates through all the blocks and checks if any of the joints are broken (i.e. connected to a null or destroyed object).
    * If a broken joint is found, it is destroyed.
    */
    public void CleanupBrokenJoints()
    {
        foreach (var blockEntry in blocks)
        {
            Rigidbody rb = blockEntry.Value;
            if (rb == null) continue;

            FixedJoint[] joints = rb.GetComponents<FixedJoint>();
            foreach (FixedJoint joint in joints)
            {
                if (joint == null ||
                    joint.connectedBody == null ||
                    joint.connectedBody.gameObject == null ||
                    joint.connectedBody.Equals(null))
                {
                    Destroy(joint);
                }
            }
        }
    }


    // Enable or disable the physics of all blocks in the vehicle
    // Only effects the player vehicle blocks, not AI since those are stored in a different manager.
    public void EnableVehiclePhysics()
    {
        foreach (Rigidbody rb in blocks.Values)
        {
            // Debug.Log("Enabling physics for block " + rb.gameObject.name);
            if (rb != null)
            {
                // Debug.Log("Enabling physics for block " + rb.gameObject.name);
                rb.isKinematic = false;
                rb.WakeUp();
            }
        }
    }

    // Disable the physics of all blocks in the vehicle
    // Only effects the player vehicle blocks, not AI since those are stored in a different manager.
    public void DisableVehiclePhysics()
    {
        foreach (Rigidbody rb in blocks.Values)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Return))
    //     {
    //         foreach (Hull block in blocks)
    //         {
    //             block.GetComponent<Rigidbody>().isKinematic = false;
    //         }
    //     }
    // }

    // Check if the core block is connected to all other blocks in the vehicle by running a BFS on the connections as edges
    // This function is used to validate the structure of the vehicle by checking if all blocks are connected to the core block.
    // It returns a HashSet of all the blocks that are connected to the core block.
    // The core block is assumed to be at the origin (0, 0, 0) in local space.
    public HashSet<Vector3Int> CoreConnectionCheck()
    {
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Vector3Int corePosition = Vector3Int.zero;
        
        queue.Enqueue(corePosition);
        visited.Add(corePosition);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            
            if (blockConnections.ContainsKey(current))
            {
                foreach (Vector3Int neighbor in blockConnections[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        
        return visited;
    }

    // Validate the structure of the vehicle by checking if all blocks are connected to the core block.
    // This function is used to validate the structure of the vehicle by checking if all blocks are connected to the core block.
    // It is called when the vehicle is built or when a block is added or removed from the vehicle.
    // It iterates through all the blocks and checks if any of them are not connected to the core block.
    // If a block is not connected, it is detached from the vehicle and its physics are enabled.
    // It also removes the block from the blocks directory and removes its connections.
    public void ValidateStructure()
    {
        if (isValidating)
            return;
        
        isValidating = true;
        HashSet<Vector3Int> connected = CoreConnectionCheck();
        List<Vector3Int> allPositions = new List<Vector3Int>(blocks.Keys); // Creating a copy of the keys (aka the block positions) to avoid modifying the dictionary while iterating over it.
        
        foreach (Vector3Int pos in allPositions)
        {
            if (!connected.Contains(pos)) // If we cannot reach a certain block from the core block, detach it.
            {
                DetachBlock(pos);
            }
        }
        isValidating = false;
    }

    // Detach a block from the vehicle and enable its physics
    // This function is used to detach a block from the vehicle and enable its physics.
    // It is called when a block is not connected to the core block and needs to be detached.
    // Param 1: pos - The local position of the block in local space as relative to the core block, in Vector3Int as x, y, z coordinates aligned to the grid.
    // It first checks if the block is in the blocks directory and if so, it enables its physics and removes its connections.
    // It also sets the block to be pickable and disables any wheel or turret scripts on the block.
    // It also destroys any fixed joints on the block.
    private void DetachBlock(Vector3Int pos)
    {
        if (blocks.TryGetValue(pos, out Rigidbody rb)) // If the block is in the blocks directory
        {
            if (rb == null) return;
            if (rb.transform.parent != null)
            {
                rb.transform.SetParent(null);
            }
            rb.isKinematic = false; // Enable physics on the block
            FixedJoint[] joints = rb.GetComponents<FixedJoint>();
            foreach (FixedJoint joint in joints)
            {
                Destroy(joint); // Destroy all fixed joints on the block
            }
            Hull hull = rb.GetComponent<Hull>();
            if (hull != null)
            {
                hull.canPickup = true; // Set the block to be pickable, aka in this scenerio it is detached
                // (Really need better naming for this variable, canPickup is not very descriptive, will fix later)
                Wheel wheel = rb.GetComponent<Wheel>();
                if (wheel != null)
                {
                    wheel.enabled = false;
                    wheel.driveInput = 0f;
                    wheel.currentSteerAngle = 0f;
                    // ^^^ Disable the wheel script and reset the drive input and steering angle
                }

                Turret turret = rb.GetComponent<Turret>();
                if (turret != null)
                {
                    turret.enabled = false;
                    if (turret.blockedLine != null)
                        turret.blockedLine.enabled = false;
                    // ^^^ Disable the turret script and the blocked line renderer
                }
                if (hull != null && hull.sourceBlock != null)
                {
                    if (vehicleBlockCounts.TryGetValue(hull.sourceBlock, out int n))
                        vehicleBlockCounts[hull.sourceBlock] = Mathf.Max(0, n - 1);

                    VehicleBuildEvents.RaiseBlockRemoved(hull.sourceBlock, pos);
                }
            }
        }
        blocks.Remove(pos); // Remove the block from the blocks directory
        RemoveConnections(pos); // Remove the connections of the block
    }

    // Recalculate the connections between blocks
    // This function is used to recalculate the connections between blocks in the vehicle.
    // It is called when a block is added or removed from the vehicle or when the vehicle is built.
    // It iterates through all the blocks and checks if any of them have fixed joints connected to other blocks.
    // If a fixed joint is found, it adds the connection to the block connections directory.
    // It also validates the structure of the vehicle by checking if all blocks are connected to the core block.
    // It also cleans up any broken joints on the blocks.
    public void recalculateConnections()
    {
        // 1. Safety check: Ensure the vehicle/grid itself hasn't been destroyed
        if (gridOrigin == null) return; 

        // First clear the current connections
        blockConnections.Clear();

        // 2. Clean up any destroyed blocks from the dictionary to prevent phantom data
        List<Vector3Int> keysToRemove = new List<Vector3Int>();
        foreach (var kvp in blocks)
        {
            if (kvp.Value == null || kvp.Value.gameObject == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            blocks.Remove(key);
        }

        // For each block, check if it has any joint connections and if so add them to the connections directory
        foreach (var blockEntry in blocks)
        {
            Rigidbody rb = blockEntry.Value;
            if (rb == null) continue;

            FixedJoint[] joints = rb.GetComponents<FixedJoint>();
            foreach (FixedJoint joint in joints)
            {
                // 3. Robust Check: Ensure the joint, the connected Rigidbody, AND its GameObject are fully valid engine objects
                if (joint != null && 
                    joint.connectedBody != null && 
                    joint.connectedBody.gameObject != null)
                {
                    Vector3Int connectedPos = Vector3Int.RoundToInt(
                        gridOrigin.InverseTransformPoint(joint.connectedBody.transform.position) // Get the connected block's position in local space
                    );
                    AddConnection(blockEntry.Key, connectedPos); // Add the connection to the directory
                }
            }
        }

        ValidateStructure();
    }

    // DelayedRecalculateConnections is a coroutine that waits for one frame and then calls the recalculateConnections method.
    // This function is used to delay the recalculation of connections to the next frame.
    // This is because broken joints take a frame to calculate, so we need to wait for the next frame to recalculate the connections.
    public IEnumerator DelayedRecalculateConnections()
    {
        yield return null;
        CleanupBrokenJoints();
        recalculateConnections();
    }
    public int GetMountedCount(Block block)
    {
        return block != null && vehicleBlockCounts.TryGetValue(block, out int n) ? n : 0;
    }
}
