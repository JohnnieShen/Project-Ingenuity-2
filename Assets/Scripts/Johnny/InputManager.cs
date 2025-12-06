using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    /* 
    * Author: Johnny
    * Summary: This script is responsible for managing input actions in the game. It uses the new Input System to handle input from the player.
    * The script is a singleton, ensuring that only one instance exists throughout the game. It initializes input actions for different modes (Build, Drive, UI) and provides methods to enable or disable these maps.
    * It also provides getter methods for accessing specific input actions.
    */

    // I don't think it's necessary to comment out this one since it's very self explanary 

    public static InputManager instance;

    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap buildMap;
    private InputActionMap driveMap;

    private InputAction buildMoveAction;
    private InputAction buildLookAction;
    private InputAction buildBuildAction;
    private InputAction buildRemoveAction;
    private InputAction buildUpAction;
    private InputAction buildDownAction;
    private InputAction buildSwapModeAction;
    private InputAction buildScrollAction;
    private InputAction buildRotateAction;
    private InputAction buildMenuAction;
    private InputAction buildSwitchToLastAction;
    private InputAction buildSwitchToNextAction;
    private InputAction buildScrollUpAction;
    private InputAction buildScrollDownAction;
    private InputAction buildRotateCameraAction;
    private InputAction driveMoveAction;
    private InputAction driveLookAction;
    private InputAction driveShootAction;
    private InputAction driveSwapModeAction;
    private InputAction driveScrollAction;
    private InputAction driveBoostAction;
    private InputAction driveInteractAction;
    private InputActionMap UIMap;
    private InputAction UIClickAction;
    private InputAction UIScrollAction;
    private InputAction UIMenuAction;
    private InputAction UIEscapeAction;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        // DontDestroyOnLoad(this.gameObject); // breaks respawning

        buildMap = inputActions.FindActionMap("Build");
        driveMap = inputActions.FindActionMap("Drive");
        UIMap = inputActions.FindActionMap("UI");

        if (buildMap != null)
        {
            buildMoveAction = buildMap.FindAction("Move");
            buildLookAction = buildMap.FindAction("Look");
            buildBuildAction = buildMap.FindAction("Build");
            buildRemoveAction = buildMap.FindAction("Remove");
            buildUpAction = buildMap.FindAction("Up");
            buildDownAction = buildMap.FindAction("Down");
            buildSwapModeAction = buildMap.FindAction("Swap Mode");
            buildScrollAction = buildMap.FindAction("Scroll");
            buildRotateAction = buildMap.FindAction("Rotate");
            buildMenuAction = buildMap.FindAction("Menu");
            buildSwitchToLastAction = buildMap.FindAction("Switch to Last Block");
            buildSwitchToNextAction = buildMap.FindAction("Switch to Next Block");
            buildScrollUpAction = buildMap.FindAction("Scroll Up");
            buildScrollDownAction = buildMap.FindAction("Scroll Down");
            buildRotateCameraAction = buildMap.FindAction("RotateCamera");
        }
        if (driveMap != null)
        {
            driveMoveAction = driveMap.FindAction("Move");
            driveLookAction = driveMap.FindAction("Look");
            driveShootAction = driveMap.FindAction("Fire");
            driveSwapModeAction = driveMap.FindAction("Swap Mode");
            driveScrollAction = driveMap.FindAction("Scroll");
            driveBoostAction = driveMap.FindAction("Boost");
            driveInteractAction = driveMap.FindAction("Interact");
        }
        if (UIMap != null)
        {
            UIClickAction = UIMap.FindAction("Click");
            UIScrollAction = UIMap.FindAction("ScrollWheel");
            UIMenuAction = UIMap.FindAction("Menu");
            UIEscapeAction = UIMap.FindAction("Escape");
        }

        EnableBuildMap();
    }
    public InputAction GetBuildMoveAction()
    {
        return buildMoveAction;
    }
    public InputAction GetBuildLookAction()
    {
        return buildLookAction;
    }
    public InputAction GetBuildBuildAction()
    {
        return buildBuildAction;
    }
    public InputAction GetBuildRemoveAction()
    {
        return buildRemoveAction;
    }
    public InputAction GetBuildUpAction()
    {
        return buildUpAction;
    }
    public InputAction GetBuildDownAction()
    {
        return buildDownAction;
    }
    public InputAction GetBuildSwapModeAction()
    {
        return buildSwapModeAction;
    }
    public InputAction GetBuildScrollAction()
    {
        return buildScrollAction;
    }
    public InputAction GetBuildRotateAction()
    {
        return buildRotateAction;
    }
    public InputAction GetBuildMenuAction()
    {
        return buildMenuAction;
    }
    public InputAction GetBuildSwitchToLastAction()
    {
        return buildSwitchToLastAction;
    }
    public InputAction GetBuildSwitchToNextAction()
    {
        return buildSwitchToNextAction;
    }
    public InputAction GetBuildScrollUpAction()
    {
        return buildScrollUpAction;
    }
    public InputAction GetBuildScrollDownAction()
    {
        return buildScrollDownAction;
    }
    public InputAction GetBuildRotateCameraAction()
    {
        return buildRotateCameraAction;
    }
    public InputAction GetDriveMoveAction()
    {
        return driveMoveAction;
    }
    public InputAction GetDriveLookAction()
    {
        return driveLookAction;
    }
    public InputAction GetDriveShootAction()
    {
        return driveShootAction;
    }
    public InputAction GetDriveSwapModeAction()
    {
        return driveSwapModeAction;
    }
    public InputAction GetDriveScrollAction()
    {
        return driveScrollAction;
    }
    public InputAction GetDriveBoostAction() {
        return driveBoostAction;
    }
    public InputAction GetDriveInteractAction() {
        return driveInteractAction;
    }
    public InputAction GetUIClickAction()
    {
        return UIClickAction;
    }
    public InputAction GetUIScrollAction()
    {
        return UIScrollAction;
    }
    public InputAction GetUIMenuAction()
    {
        return UIMenuAction;
    }
    public InputAction GetUIEscapeAction()
    {
        return UIEscapeAction;
    }
    public void EnableBuildMap()
    {
        if (UIMap != null) UIMap.Disable();
        if (buildMap != null) buildMap.Enable();
        if (driveMap != null) driveMap.Disable();
    }

    public void EnableDriveMap()
    {
        if (UIMap != null) UIMap.Disable();
        if (driveMap != null) driveMap.Enable();
        if (buildMap != null) buildMap.Disable();
    }
    public void EnableUIMap()
    {
        if (UIMap != null) UIMap.Enable();
        if (buildMap != null) buildMap.Disable();
        if (driveMap != null) driveMap.Disable();
    }
}
