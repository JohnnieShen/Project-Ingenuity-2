using UnityEngine;

public class AITestSpawner : MonoBehaviour
{
    [Header("Test Settings")]
    public string jsonSaveName = "MySavedVehicle"; // Type the exact name of a vehicle you saved
    public Transform spawnPoint; // Where to drop the enemy
    public KeyCode spawnKey = KeyCode.T; // Press 'T' to spawn

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            if (AIVehicleSpawner.instance == null)
            {
                Debug.LogError("AIVehicleSpawner is missing from the scene!");
                return;
            }

            // Spawn 15 units in front of this object if no spawn point is assigned
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward * 15f;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            Debug.Log($"Spawning AI Enemy from save: {jsonSaveName}");
            AIVehicleSpawner.instance.SpawnAIVehicle(jsonSaveName, pos, rot);
        }
    }
}