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

        string path = Application.persistentDataPath;
        DirectoryInfo dir = new DirectoryInfo(path);
        
        if (!dir.Exists) return;

        FileInfo[] info = dir.GetFiles("*.json");

        foreach (FileInfo f in info)
        {
            string cleanName = Path.GetFileNameWithoutExtension(f.Name);

            GameObject newBtnObj = Instantiate(saveFileButtonPrefab, saveListContentParent);
            VehicleSaveEntryUI entryUI = newBtnObj.GetComponent<VehicleSaveEntryUI>();

            if (entryUI != null)
            {
                // Set the text
                if (entryUI.fileNameText != null)
                {
                    entryUI.fileNameText.text = cleanName;
                }

                // Bind the Load button
                if (entryUI.loadButton != null)
                {
                    entryUI.loadButton.onClick.RemoveAllListeners();
                    entryUI.loadButton.onClick.AddListener(() => LoadSelectedVehicle(cleanName));
                }

                // Bind the Delete button
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