using UnityEngine;
using System;
//using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class FreeCameraLook : Pivot {
	/*
	* Author: Johnny
	* Summary: This script is responsible for controlling the free camera movement and look in the game.
	* It allows the player to move the camera around, look around, and zoom in and out.
	* It also handles the firing action and interacts with the game world by picking up blocks.
	* The script uses Unity's Input System for handling input actions and events.
	*/

	public static event Action OnFire;

	public static FreeCameraLook instance;
	[SerializeField] private float moveSpeed = 5f;
	[SerializeField] private float turnSpeed = 1.5f;
	[SerializeField] private float turnsmoothing = .1f;
	[SerializeField] private float tiltMax = 75f;
	[SerializeField] private float tiltMin = 45f;
	[SerializeField] private float zoomSpeed = 10f;
	[SerializeField] private float minZoom = 5f;
	[SerializeField] private float maxZoom = 30f;
	[SerializeField] private float zoomSmoothFactor = 5f;
	[SerializeField] private LayerMask terrainLayerMask;
	// [SerializeField] private float collisionOffset = 0.5f;

    [Header("Pivot Settings")]
    [SerializeField] private float pivotMoveSpeed = 0.05f;
    [SerializeField] private float driveModePivotY = 4f;
    [SerializeField] private float minPivotY = -20f;
    [SerializeField] private float maxPivotY = 20f;

    public Transform aimTarget;
	public Transform commandModule;
    public LayerMask rayCastLayers;
    public LayerMask shieldLayer;

	private float targetZoom = 10f;
	private float currentZoom = 10f;
	// [SerializeField] private bool lockCursor = false;

	private float lookAngle;
	private float tiltAngle;

	private const float LookDistance = 100f;

	private float smoothX = 0;
	private float smoothY = 0;
	private float smoothXvelocity = 0;
	private float smoothYvelocity = 0;
	private bool isFiring = false;
	public float terrainDetectionRangeMultiplyer = 1f;
	public float pickUpDistance = 5f;
	public GameObject pickupIconPanel;
	public Image pickupIconImage;

	private float _initialZoom;
    private float _initialTilt;
    private float _initialYaw;

    private float scrollDistance = 0f;

    private bool isInBuildMode = false;

    /* Awake is called when the script instance is being loaded.
	* It initializes the camera and pivot transforms, sets the target zoom, and checks for duplicate instances of the FreeCameraLook script.
	* It also creates a new AimTarget GameObject if it is not assigned.
	*/
    protected override void Awake()
	{
		base.Awake();

        Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	
		cam = GetComponentInChildren<Camera>().transform;
		pivot = cam.parent;
		targetZoom = currentZoom;
		if (instance == null)
            instance = this;
        else
            Debug.LogError("Duplicated FreeCam", gameObject);
		if (aimTarget == null) {
            GameObject aimTargetGO = new GameObject("AimTarget");
            aimTarget = aimTargetGO.transform;
            Debug.Log("aimTarget was not assigned. A new AimTarget GameObject has been created.");
        }
		_initialZoom = currentZoom;
		_initialTilt = tiltAngle;
		_initialYaw  = lookAngle;
	}

	/* OnEnable sets up the listeners for the drive shoot and scroll actions from the InputManager.
	* It subscribes to the performed and canceled events for the drive shoot action and the performed event for the drive scroll action.
	* It also checks if the InputManager instance is not null and if the actions are not null before subscribing to the events.
	*/
	private void OnEnable()
    {
        if (InputManager.instance != null)
        {
            if (InputManager.instance.GetDriveShootAction() != null)
            {
                InputManager.instance.GetDriveShootAction().performed += OnFireStarted;
                InputManager.instance.GetDriveShootAction().canceled += OnFireCanceled;
            }
            if (InputManager.instance.GetDriveScrollAction() != null)
            {
                InputManager.instance.GetDriveScrollAction().performed += OnScrollPerformed;
            }

            if (InputManager.instance.GetBuildScrollAction() != null)
            {
                InputManager.instance.GetBuildScrollAction().performed += OnScrollPerformed;
            }
        }
    }

	/* OnDisable removes the listeners for the drive shoot and scroll actions from the InputManager.
	* It unsubscribes from the performed and canceled events for the drive shoot action and the performed event for the drive scroll action.
	* It also checks if the InputManager instance is not null and if the actions are not null before unsubscribing from the events.
	*/
    private void OnDisable()
    {
        if (InputManager.instance != null)
        {
            if (InputManager.instance.GetDriveShootAction() != null)
            {
                InputManager.instance.GetDriveShootAction().performed -= OnFireStarted;
                InputManager.instance.GetDriveShootAction().canceled -= OnFireCanceled;
            }
            if (InputManager.instance.GetDriveScrollAction() != null)
            {
                InputManager.instance.GetDriveScrollAction().performed -= OnScrollPerformed;
            }

            if (InputManager.instance.GetBuildScrollAction() != null)
            {
                InputManager.instance.GetBuildScrollAction().performed -= OnScrollPerformed;
            }
        }
    }

    public void SetBuildMode(bool state)
    {
        isInBuildMode = state;
    }

    /* OnFireStarted is called when the drive shoot action is performed.
	* It sets the isFiring variable to true, indicating that the player is firing.
	* It also checks if the InputManager instance is not null and if the action is not null before setting the variable.
	* Param 1: ctx - The input action context that contains information about the input event.
	*/
    void OnFireStarted(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isFiring = true;
    }

	/* OnFireCanceled is called when the drive shoot action is canceled.
	* It sets the isFiring variable to false, indicating that the player has stopped firing.
	* It also checks if the InputManager instance is not null and if the action is not null before setting the variable.
	* Param 1: ctx - The input action context that contains information about the input event.
	*/
    void OnFireCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isFiring = false;
    }
	
	// Update is called once per frame
	// It handles the camera rotation, zoom, and target updates.
	// It also checks for mouse input and interacts with the game world by picking up blocks.
	// It uses the InputManager instance to get the drive look and interact actions.
	// It also checks for the pickup icon panel and updates it based on the hovered hull.
	protected override void Update ()
	{
		base.Update();

        if (!isInBuildMode)
        {
            if (pivot != null)
            {
                pivot.localPosition = new Vector3(0f, driveModePivotY, 0f);
            }
            scrollDistance = 0f;
        }
        else
        {
            if (pivot != null)
            {
                float newY = 5f + scrollDistance;
                newY = Mathf.Clamp(newY, minPivotY, maxPivotY);
                pivot.localPosition = new Vector3(0f, newY, 0f);
            }
        }

        HandleRotationMovement();
		HandleZoom2();
		UpdateTarget();
        // if (lockCursor && Input.GetMouseButtonUp (0))
        // {
        //     Cursor.lockState = CursorLockMode.Confined;
        // }
        if (!isInBuildMode)
        {
            if (Input.GetMouseButtonDown(0)) isFiring = true;
            if (Input.GetMouseButtonUp(0)) isFiring = false;
            if (isFiring) OnFire?.Invoke();
        }
        Ray forwardRay;
        if (isInBuildMode)
        {
            forwardRay = cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            forwardRay = cam.GetComponent<Camera>().ScreenPointToRay(centerScreen);
        }
        Hull hoveredHull = FindHoveredHull(forwardRay);
		if(pickupIconPanel != null) {
			bool showPickupPanel = (hoveredHull != null);
			pickupIconPanel.SetActive(showPickupPanel);
			if(showPickupPanel && pickupIconImage != null && hoveredHull.sourceBlock != null) {
				pickupIconImage.sprite = hoveredHull.sourceBlock.uiSprite;
			}
		}
		// if (hoveredHull != null) {
		// 	Debug.Log("Hovered Hull: " + hoveredHull.name);
		// }
		if (InputManager.instance != null
			&& InputManager.instance.GetDriveInteractAction() != null 
			&& InputManager.instance.GetDriveInteractAction().triggered 
			&& hoveredHull != null)
		{
			PickupBlock(hoveredHull);
		}
	}

    /* Follow is called to update the camera position based on the target's position.
	* It uses linear interpolation to smoothly move the camera towards the target position.
	* Param 1: deltaTime - The time since the last frame, used for smooth movement.
	*/
    protected override void Follow(float deltaTime)
    {
        Transform center = commandModule != null ? commandModule : target;
        if (center == null) return;

        transform.position = Vector3.Lerp(transform.position, center.position, deltaTime * moveSpeed);
    }

    /* UpdateTarget is called to update the aim target position based on the camera's forward direction.
	* It uses a raycast to check for collisions with the terrain or enemy blocks.
	* If a collision is detected, it updates the aim target position to the hit point.
	* If no collision is detected, it sets the aim target position to a point far away in the forward direction.
	* It also draws a debug ray to visualize the raycast direction.
	*/
    private void UpdateTarget() 
	{
        Ray forwardRay;
        if (isInBuildMode)
        {
            forwardRay = cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            forwardRay = cam.GetComponent<Camera>().ScreenPointToRay(centerScreen);
        }
        // Ray forwardRay = cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(forwardRay.origin, forwardRay.direction * 1000f, Color.red, 0.1f);

		//int aimLayer = LayerMask.NameToLayer("Aim");
		//if (aimLayer == -1)
		//{
		//	Debug.LogError("Layer 'Aim' does not exist in Project Settings > Tags and Layers.");
		//	return;
		//}
		//int aimMask = 1 << aimLayer;

		RaycastHit hit;
		if (Physics.Raycast(forwardRay, out hit, 1000f, LayerMask.GetMask("Terrain", "EnemyBlock"), QueryTriggerInteraction.Collide))
		{
			//Debug.Log("Hit: " + hit.transform.name + " tag: " + hit.transform.tag);
			aimTarget.position = Vector3.Lerp(aimTarget.position, hit.point, Time.deltaTime * 10f);
		}
		else
		{
			aimTarget.position = forwardRay.origin + forwardRay.direction * 1000f;
		}
	}


		
    /* HandleRotationMovement is called to handle the camera rotation and movement based on player input.
	* It uses the InputManager instance to get the drive look action and reads the input values.
	* It applies smoothing to the input values if the turnsmoothing variable is greater than 0.
	* It updates the look angle and tilt angle based on the input values and applies clamping to the tilt angle.
	* It also updates the camera and pivot rotations based on the calculated angles.
	*/
	void HandleRotationMovement()
    {
        bool allowRotation = true;

        if (isInBuildMode)
        {
            if (!Input.GetMouseButton(2))
            {
                allowRotation = false;
            }
        }

        if (!allowRotation) return;

        Vector2 lookVal = Vector2.zero;
        if (InputManager.instance != null && InputManager.instance.GetDriveLookAction() != null && InputManager.instance.GetDriveLookAction().enabled)
        {
            lookVal = InputManager.instance.GetDriveLookAction().ReadValue<Vector2>();
        }
        else if (InputManager.instance != null && InputManager.instance.GetBuildLookAction() != null && InputManager.instance.GetBuildLookAction().enabled)
        {
            lookVal = InputManager.instance.GetBuildLookAction().ReadValue<Vector2>();
        }

        float x = lookVal.x;
        float y = lookVal.y;

        if (turnsmoothing > 0)
        {
            smoothX = Mathf.SmoothDamp(smoothX, x, ref smoothXvelocity, turnsmoothing);
            smoothY = Mathf.SmoothDamp(smoothY, y, ref smoothYvelocity, turnsmoothing);
        }
        else
        {
            smoothX = x;
            smoothY = y;
        }

        lookAngle += smoothX * turnSpeed;
        transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);

        tiltAngle -= smoothY * turnSpeed;
        tiltAngle = Mathf.Clamp(tiltAngle, -tiltMin, tiltMax);
        pivot.localRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
    }

    /* OnScrollPerformed is called when the drive scroll action is performed.
	* It reads the scroll value from the input action context and updates the target zoom based on the scroll value.
	* It clamps the target zoom value between the minimum and maximum zoom values.
	* Param 1: ctx - The input action context that contains information about the input event.
	*/
    private void OnScrollPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 scrollValue = ctx.ReadValue<Vector2>();
        float scroll = scrollValue.y;

        bool isAltHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        //bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isInBuildMode && isAltHeld)
        {
            if (pivot != null)
            {
                scrollDistance += (scroll * pivotMoveSpeed);
            }
        }
        else if (!isInBuildMode || isInBuildMode)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    /* HandleZoom is called to handle the camera zoom based on the target zoom value.
	* It uses linear interpolation to smoothly move the camera towards the target zoom value.
	* It calculates the desired local position based on the current zoom value and the pivot transform.
	* It performs a raycast to check for collisions with the terrain and updates the current zoom value based on the hit distance.
	* It clamps the current zoom value between the minimum and maximum zoom values.
	* It updates the camera local position based on the desired local position and the current zoom value.
	*/
    void HandleZoom()
	{
		currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSmoothFactor);
		Vector3 desiredLocalPos = new Vector3(0f, 0f, -currentZoom);
		Vector3 desiredWorldPos = pivot.TransformPoint(desiredLocalPos);

		Vector3 direction = (desiredWorldPos - pivot.position).normalized;
		float distance = Vector3.Distance(pivot.position, desiredWorldPos);

		RaycastHit hit;
		if (Physics.Raycast(pivot.position, direction, out hit, distance*terrainDetectionRangeMultiplyer, terrainLayerMask))
		{
			currentZoom = hit.distance - 1f;
			currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
			desiredLocalPos = new Vector3(0f, 2f, -currentZoom);
		}
		cam.localPosition = Vector3.Lerp(cam.localPosition, desiredLocalPos, Time.deltaTime * zoomSmoothFactor);
	}

	void HandleZoom2()
	{
		currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSmoothFactor);
		Vector3 desiredLocalPos = new Vector3(0f, 0f, -currentZoom);
		Vector3 desiredWorldPos = pivot.TransformPoint(desiredLocalPos);

		Vector3 direction = (desiredWorldPos - pivot.position).normalized;
		float distance = Vector3.Distance(pivot.position, desiredWorldPos);

		RaycastHit hit;
		if (Physics.Raycast(pivot.position, direction, out hit, distance * terrainDetectionRangeMultiplyer, terrainLayerMask))
		{
			// Add a small offset to prevent clipping into the terrain
			float safeDistance = Mathf.Max(hit.distance - 0.5f, minZoom);
			currentZoom = Mathf.Min(safeDistance, targetZoom);
			desiredLocalPos = new Vector3(0f, 0f, -currentZoom);
		}

		cam.localPosition = Vector3.Lerp(cam.localPosition, desiredLocalPos, Time.deltaTime * zoomSmoothFactor);
	}

	/* FindHoveredHull is called to find the hull that is currently hovered by the camera.
	* It performs a raycast from the camera's position in the forward direction and checks for collisions with the specified layers.
	* If a collision is detected, it checks if the hit object is a block or enemy block and if it can be picked up.
	* If so, it returns the Hull component of the hit object.
	* Otherwise, it returns null.
	* Param 1: ray - The ray to cast from the camera's position in the forward direction.
	* Returns: The Hull component of the hovered object or null if no hull is found.
	*/
	private Hull FindHoveredHull(Ray ray)
    {
        LayerMask combinedMask = rayCastLayers & ~shieldLayer;
        if (Physics.Raycast(ray, out RaycastHit hitInfo, pickUpDistance, combinedMask))
        {
			// Debug.Log("Hit: " + hitInfo.transform.name);
            if ((hitInfo.transform.CompareTag("Block") ||
               hitInfo.transform.CompareTag("EnemyBlock")) && hitInfo.transform.GetComponent<Hull>().canPickup)
            {
                return hitInfo.transform.GetComponent<Hull>();
            }
        }
        return null;
    }

	/* PickupBlock is called to pick up a block from the game world.
	* It checks if the hull and source block are not null before proceeding.
	* It adds the block to the BlockInventoryManager and destroys the hull game object.
	* It also checks if the BlockInventoryManager instance is not null before adding the block.
	* Param 1: hull - The Hull component of the block to be picked up.
	*/
	private void PickupBlock(Hull hull)
    {
        if (hull == null || hull.sourceBlock == null) {
			// Debug.LogWarning("Hull or sourceBlock is null. Cannot pick up block.");
			return;
		}

        BlockInventoryManager.instance.AddBlock(hull.sourceBlock, 1);
		//Remove blocks from manager here?

        Destroy(hull.gameObject);
    }
    public void ResetView()
    {
        Transform center = commandModule != null ? commandModule : target;
        if (center == null) return;

        lookAngle = center.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, lookAngle, 0f);

        tiltAngle = 0f;
        pivot.localRotation = Quaternion.Euler(tiltAngle, 0f, 0f);

        targetZoom = _initialZoom;
        currentZoom = _initialZoom;
        cam.localPosition = new Vector3(0f, 0f, -currentZoom);

        // Snap pivot onto the center
        transform.position += center.position - pivot.position;
    }
}
