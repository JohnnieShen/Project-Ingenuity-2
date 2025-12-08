using UnityEngine;
using System.Collections;
using System;

public class ModeSwitcher : MonoBehaviour
{
    /*
    * Author: Johnny
    * Summary: This script manages the switching between different modes in the game, such as Build, Drive, and Craft modes.
    * It handles the UI visibility, player controls, and vehicle physics based on the current mode.
    * The script uses a singleton pattern to ensure only one instance exists and provides methods to set the current mode.
    */

    public static ModeSwitcher instance;
    public BuildSystem build;
    public enum Mode { Build, Drive , Craft };
    public Mode currentMode = Mode.Drive;

    public event Action<Mode, Mode> OnModeChanged;

    public GameObject player;

    // public GameObject vehicle;
    public GameObject driveCameraPivot;
    public Transform vehicleRoot;
    public Transform drivingCamera;
    public GameObject buildUI;
    public GameObject driveUI;
    public GameObject craftUI;
    public GameObject cursorUI;
    public GameObject winUI;
    public float buildModeHeight = 5f;
    public float elevateDuration = 1f;
    public bool canManuallySwitchMode = true;
    public float modeSwitchCooldown = 0.5f;
    private float lastModeSwitchTime = 0f;
    private Vector3 originalVehiclePosition;
    private Quaternion originalVehicleRotation;
    public GameObject buildArrow;

    /* Awake is called when the script instance is being loaded.
    * It initializes the singleton instance and ensures that only one instance of the ModeSwitcher exists.
    */
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    /* Start is called before the first frame update.
    * It sets the original vehicle position and rotation, and calls the SetMode method to set the initial mode.
    */
    void Start()
    {
        if(vehicleRoot != null)
        {
            originalVehiclePosition = vehicleRoot.position;
            originalVehicleRotation = vehicleRoot.localRotation;
        }
        SetMode(currentMode);
    }

    /* Update is called once per frame.
    * It checks for input to switch modes and calls the SetMode method accordingly.
    * It also handles the elevation of the vehicle when switching to Build mode.
    */
    void Update()
    {
        if (!canManuallySwitchMode || Time.time - lastModeSwitchTime < modeSwitchCooldown)
            return;

        if (canManuallySwitchMode)
        {
            bool buildSwapTriggered = InputManager.instance.GetBuildSwapModeAction() != null && InputManager.instance.GetBuildSwapModeAction().triggered;
            bool driveSwapTriggered = InputManager.instance.GetDriveSwapModeAction() != null && InputManager.instance.GetDriveSwapModeAction().triggered;

            if (buildSwapTriggered || driveSwapTriggered)
            {
                if (currentMode == Mode.Drive)
                {
                    // Refactored: Call SetMode directly. Let SetMode handle the assignment.
                    SetMode(Mode.Build);

                    lastModeSwitchTime = Time.time;
                    if (vehicleRoot != null)
                    {
                        StopAllCoroutines();
                        StartCoroutine(ElevateVehicle(buildModeHeight, elevateDuration));
                    }
                    return;
                }
                else if (currentMode == Mode.Build)
                {
                    // Refactored: Call SetMode directly.
                    SetMode(Mode.Drive);

                    lastModeSwitchTime = Time.time;
                    return;
                }
            }
        }
        if (canManuallySwitchMode && currentMode == Mode.Build)
        {
            if (InputManager.instance.GetBuildMenuAction() != null && InputManager.instance.GetBuildMenuAction().triggered)
            {
                SetMode(Mode.Craft);
                lastModeSwitchTime = Time.time;
                return;
            }
        }
        if (canManuallySwitchMode && currentMode == Mode.Craft)
        {
            if ((InputManager.instance.GetUIMenuAction() != null && InputManager.instance.GetUIMenuAction().triggered) || InputManager.instance.GetUIEscapeAction() != null && InputManager.instance.GetUIEscapeAction().triggered)
            {
                SetMode(Mode.Build);

                if (build != null)
                    build.RefreshSelection();
                lastModeSwitchTime = Time.time;
                return;
            }
        }
    }

    /* SetMode is called to set the current mode of the game.
    * It takes a Mode parameter and sets the appropriate UI visibility, player controls, and vehicle physics based on the mode.
    * It also handles the visibility of the build arrow and the elevation of the vehicle when switching to Build mode.
    * Param 1: mode - The mode to be set (Build, Drive, or Craft).
    */
    public void SetMode(Mode newMode)
    {
        // Capture previous mode before changing
        Mode previousMode = currentMode;
        currentMode = newMode;

        if (FreeCameraLook.instance != null)
        {
            FreeCameraLook.instance.SetBuildMode(currentMode == Mode.Build || currentMode == Mode.Craft);
        }

        if (currentMode == Mode.Build)
        {
            buildArrow.SetActive(true);
            cursorUI.SetActive(false);
            // Time.timeScale = 1f;
            if (buildUI != null) buildUI.SetActive(true);
            if (driveUI != null) driveUI.SetActive(false);
            if (craftUI != null) craftUI.SetActive(false);
            InputManager.instance.EnableBuildMap();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (player != null) player.SetActive(false);

            if (driveCameraPivot != null) driveCameraPivot.SetActive(true);

            if (BlockManager.instance != null)
            {
                BlockManager.instance.DisableVehiclePhysics();
            }

            if (EnemyBlockManager.instance != null)
            {
                foreach (EnemyAI enemy in EnemyBlockManager.instance.GetEnemyVehicles())
                {
                    if (enemy != null) enemy.enabled = false;
                }
            }
        }
        // Vice versa
        else if (currentMode == Mode.Drive)
        {
            buildArrow.SetActive(false);
            cursorUI.SetActive(true);
            // Time.timeScale = 1f;
            if (buildUI != null) buildUI.SetActive(false);
            if (driveUI != null) driveUI.SetActive(true);
            if (craftUI != null) craftUI.SetActive(false);
            InputManager.instance.EnableDriveMap();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (driveCameraPivot != null) driveCameraPivot.SetActive(true);
            if (build != null) build.destroyPreviewBlock();
            if (player != null) player.SetActive(false);

            if (BlockManager.instance != null)
            {
                BlockManager.instance.EnableVehiclePhysics();
            }
            if (EnemyBlockManager.instance != null)
            {
                foreach (EnemyAI enemy in EnemyBlockManager.instance.GetEnemyVehicles())
                {
                    if (enemy != null) enemy.enabled = true;
                }
            }
        }
        else if (currentMode == Mode.Craft)
        {
            buildArrow.SetActive(true);
            // Time.timeScale = 0f;
            if (buildUI != null) buildUI.SetActive(false);
            if (driveUI != null) driveUI.SetActive(false);
            if (craftUI != null) craftUI.SetActive(true);
            InputManager.instance.EnableUIMap();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Emit the event after all logic is handled
        OnModeChanged?.Invoke(previousMode, currentMode);
    }

    /* ElevateVehicle is a coroutine that smoothly elevates the vehicle to a target height over a specified duration.
    * It uses Lerp to interpolate the position and rotation of the vehicle root transform.
    * Param 1: targetHeight - The target height to elevate the vehicle to.
    * Param 2: duration - The duration over which to elevate the vehicle.
    */
    IEnumerator ElevateVehicle(float targetHeight, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = vehicleRoot.position;
        Vector3 endPos = new Vector3(startPos.x, startPos.y + targetHeight, startPos.z);
        Quaternion startRot = vehicleRoot.localRotation;
        Quaternion endRot = Quaternion.identity;
        // Debug.Log("Rotating vehicle from " + startRot + " to " + endRot);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            vehicleRoot.position = Vector3.Lerp(startPos, endPos, t);
            vehicleRoot.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        vehicleRoot.position = endPos;
        vehicleRoot.localRotation = endRot;
    }

    public void hideAllUI()
    {
        if (buildUI != null) buildUI.SetActive(false);
        if (driveUI != null) driveUI.SetActive(false);
        if (craftUI != null) craftUI.SetActive(false);
        if (cursorUI != null) cursorUI.SetActive(false);
        if (winUI != null) winUI.SetActive(false);
    }
}
