using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VehicleSaveUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField fileNameInput;
    public Button saveButton;
    public Transform saveListContentParent;
    public GameObject saveFileButtonPrefab; // Ensure this prefab has the VehicleSaveEntryUI script attached

    private void OnEnable()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveCurrentVehicle);
        }

        RefreshSaveList();
    }

    public void SaveCurrentVehicle()
    {
        string fileName = fileNameInput.text.Trim();
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogWarning("File name cannot be empty!");
            return;
        }

        VehicleSaveSystem.instance.SaveVehicle(fileName);
        
        fileNameInput.text = ""; 
        RefreshSaveList(); 
    }

    public void LoadSelectedVehicle(string fileName)
    {
        VehicleSaveSystem.instance.LoadVehicle(fileName);
    }

    public void DeleteSelectedVehicle(string fileName)
    {
        VehicleSaveSystem.instance.DeleteVehicle(fileName);
        RefreshSaveList();
    }

    public void RefreshSaveList()
    {
        if (saveListContentParent == null || saveFileButtonPrefab == null) return;

        // Clear old entries
        foreach (Transform child in saveListContentParent)
        {
            Destroy(child.gameObject);
        }

        string path = Path.Combine(Application.persistentDataPath, "PlayerVehicles");
        DirectoryInfo dir = new DirectoryInfo(path);
        
        if (!dir.Exists) return;

        FileInfo[] info = dir.GetFiles("*.json");

        foreach (FileInfo f in info)
        {
            string cleanName = Path.GetFileNameWithoutExtension(f.Name);

            string displayName = cleanName;
            if (displayName.Length > 10)
            {
                displayName = displayName.Substring(0, 10) + "...";
            }

            GameObject newBtnObj = Instantiate(saveFileButtonPrefab, saveListContentParent);
            VehicleSaveEntryUI entryUI = newBtnObj.GetComponent<VehicleSaveEntryUI>();

            if (entryUI != null)
            {
                if (entryUI.fileNameText != null)
                {
                    entryUI.fileNameText.text = displayName;
                }

                if (entryUI.loadButton != null)
                {
                    entryUI.loadButton.onClick.RemoveAllListeners();
                    entryUI.loadButton.onClick.AddListener(() => LoadSelectedVehicle(cleanName));
                }

                if (entryUI.deleteButton != null)
                {
                    entryUI.deleteButton.onClick.RemoveAllListeners();
                    entryUI.deleteButton.onClick.AddListener(() => DeleteSelectedVehicle(cleanName));
                }
            }
            else
            {
                Debug.LogWarning("VehicleSaveEntryUI script is missing on the instantiated prefab.");
            }
        }
    }
}