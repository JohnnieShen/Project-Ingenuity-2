using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildSystem : MonoBehaviour
{
    /*
    * Author: Johnny
    * Summary: This script manages the building system in the game. It allows players to build and destroy blocks in the game world.
    * It handles the selection of blocks, the placement of blocks in the world, and the removal of blocks.
    * The script uses raycasting to determine where the player is looking and where to place or remove blocks.
    * It also manages the UI elements related to block selection and placement.
    */

    // public Block[] availableBuildingBlocks;

    public static BuildSystem instance;

    private int currentRow = 0;
    private int currentColumn = 0;
 
    Block currentBlock;
    public TMP_Text blockNameText;
    public TMP_Text blockDescriptionText;

    public Image blockUIImage;
    public int breakForce;
 
    public Transform shootingPoint;
    GameObject blockObject;

    public Camera buildCamera;

    public Transform parent;
 
    public Color normalColor;
    public Color highlightedColor;
 
    GameObject lastHightlightedBlock;
    public LayerMask rayCastLayers;
    public LayerMask shieldLayer;
    public LayerMask previewIgnoreLayers;
    
    public Material previewMaterial;
    private GameObject previewBlock;
    private Quaternion previewBlockOriginalRotation;
    private int rotationOffsetCount = 0;
    // public BlockInventory[] availableBuildingBlocks;
    // private Dictionary<Block, BlockInventory> blockInventory = new Dictionary<Block, BlockInventory>();
    private List<Block> availableBlocksList = new List<Block>();

    [Header("References")]
    public Transform commandModule;
    private Transform referenceTransform;
    private System.Action<InputAction.CallbackContext> onPrevLevel;
    private System.Action<InputAction.CallbackContext> onNextLevel;
    /* OnEnable is called when the object becomes enabled and active.
    * It sets up the input actions for building, removing, scrolling, and rotating blocks.
    * It also initializes the block inventory and sets the initial block to be built.
    * The reference transform is set to the position and rotation of the command module.
    * The blocks are reparented to the reference transform and then transferred back to the parent.
    */

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip audioClip;

    private void OnEnable()
    {
        UpdateCurrentBlockFromMatrix();
        SetText();
        if (InputManager.instance != null)
        {
            InputManager.instance.GetBuildBuildAction().performed += OnBuildPerformed;
            InputManager.instance.GetBuildRemoveAction().performed += OnRemovePerformed;
            //InputManager.instance.GetBuildScrollAction().performed += OnScrollPerformed;
            InputManager.instance.GetBuildRotateAction().performed += OnRotatePerformed;
            //InputManager.instance.GetBuildScrollUpAction().performed += OnScrollUpPerformed;
            //InputManager.instance.GetBuildScrollDownAction().performed += OnScrollDownPerformed;
            var prev = InputManager.instance.GetBuildSwitchToLastAction();
            var next = InputManager.instance.GetBuildSwitchToNextAction();
            onPrevLevel = ctx => ChangeColumn(-1);
            onNextLevel = ctx => ChangeColumn(+1);
            //prev.performed += onPrevLevel;
            //next.performed += onNextLevel;
        }
        if (commandModule != null)
        {
            referenceTransform.position = commandModule.position;
            referenceTransform.rotation = commandModule.rotation;
        }
        
        ReparentAllBlocksToReference();
        referenceTransform.rotation = Quaternion.identity;
        TransferBlocksToParent();
    }

    /* OnDisable is called when the behaviour becomes disabled or inactive.
    * It removes the input action listeners to prevent memory leaks and ensures that the blocks are reparented to the command module.
    */
    private void OnDisable()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.GetBuildBuildAction().performed -= OnBuildPerformed;
            InputManager.instance.GetBuildRemoveAction().performed -= OnRemovePerformed;
            //InputManager.instance.GetBuildScrollAction().performed -= OnScrollPerformed;
            InputManager.instance.GetBuildRotateAction().performed -= OnRotatePerformed;
            //InputManager.instance.GetBuildScrollUpAction().performed -= OnScrollUpPerformed;
            //InputManager.instance.GetBuildScrollDownAction().performed -= OnScrollDownPerformed;
            var prev = InputManager.instance.GetBuildSwitchToLastAction();
            var next = InputManager.instance.GetBuildSwitchToNextAction();
            //if (prev != null && onPrevLevel != null) prev.performed -= onPrevLevel;
            //if (next != null && onNextLevel != null) next.performed -= onNextLevel;
        }
    }

    /* Awake is called when the script instance is being loaded.
    * It initializes the block inventory and sets the initial block to be built.
    * It also creates a reference transform for the command module.
    * The reference transform is used to calculate the position and rotation of the blocks when they are placed in the world.
    */
    private void Awake()
    {
        if (instance == null) instance = this;
        else Debug.LogWarning("Multiple BuildSystems found!");

        // InitializeInventory();
        UpdateCurrentBlockFromMatrix();
        SetText();
        GameObject dummy = new GameObject("ReferenceTransform");
        referenceTransform = dummy.transform;
    }

    public void SelectBlock(Block block)
    {
        var matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null) return;

        for (int r = 0; r < matrix.rowsCount; r++)
        {
            var row = matrix.rows[r];
            if (row.columns == null) continue;

            for (int c = 0; c < matrix.columnsCount; c++)
            {
                var entry = row.columns[c];
                if (entry != null && entry.Block == block)
                {
                    currentRow = r;
                    currentColumn = c;

                    UpdateCurrentBlockFromMatrix();
                    SetText();
                    destroyPreviewBlock();
                    return;
                }
            }
        }
        Debug.LogWarning($"Could not find block {block.BlockName} in the inventory matrix.");
    }

    // void InitializeInventory()
    // {
    //     foreach (BlockInventory ib in availableBuildingBlocks)
    //     {
    //         blockInventory[ib.Block] = ib;
    //         if (ib.CurrentCount > 0)
    //         {
    //             availableBlocksList.Add(ib.Block);
    //         }
    //     }
    //     UpdateAvailableBlocks();
    // }

    // void UpdateAvailableBlocks()
    // {
    //     availableBlocksList.Clear();
    //     foreach (BlockInventory ib in availableBuildingBlocks)
    //     {
    //         if (ib.CurrentCount > 0)
    //         {
    //             availableBlocksList.Add(ib.Block);
    //         }
    //     }
        
    //     if (availableBlocksList.Count > 0)
    //     {
    //         currentBlockIndex = Mathf.Clamp(currentBlockIndex, 0, availableBlocksList.Count - 1);
    //         currentBlock = availableBlocksList[currentBlockIndex];
    //     }
    //     else
    //     {
    //         currentBlock = null;
    //     }
    //     SetText();
    // }

    // private void Update()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         BuildBlock(currentBlock.BlockObject);
    //     }
    //     if (Input.GetMouseButtonDown(1))
    //     {
    //         DestroyBlock();
    //     }
    //     ChangeCurrentBlock();
    //     // UpdatePreview();
    // }

    /* Update is called every frame.
    * It checks if the command module and reference transform are not null.
    * If they are not null, it updates the position and rotation of the reference transform to match the command module.
    * It also updates the preview of the block being built.
    * The preview is a visual representation of the block that will be placed in the world.
    */
    private void Update()
    {
        if (commandModule != null && referenceTransform != null)
        {
            referenceTransform.position = commandModule.position;
            referenceTransform.rotation = commandModule.rotation;
        }
        UpdatePreview();
    }

    private Ray GetBuildRay()
    {
        if (buildCamera != null)
        {
            Debug.Log("lol");
            return buildCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        }
        return new Ray(shootingPoint.position, shootingPoint.forward);
    }

    /* DestroyPreviewBlock is called to destroy the preview block if it exists.
    */
    public void destroyPreviewBlock()
    {
        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }
    }
 
    /* OnBuildPerformed is called when the build action is performed.
    * It checks if the current block is not null and if it has a block object.
    * If it does, it calls the BuildBlock method to place the block in the world.
    * If the current block is null or has no block object, it logs a warning message.
    * Param 1: ctx - The input action callback context.
    */
    private void OnBuildPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("build perform");
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (currentBlock == null || currentBlock.BlockObject == null)
        {
            Debug.LogWarning("No block selected yet!");
            return;
        }
        BuildBlock(currentBlock.BlockObject);
    }

    /* OnRemovePerformed is called when the remove action is performed.
    * It calls the DestroyBlock method to remove a block from the world.
    * Param 1: ctx - The input action callback context.
    */
    private void OnRemovePerformed(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        DestroyBlock();
    }

    /* OnScrollPerformed is called when the scroll action is performed.
    * It checks the scroll value and updates the current block index accordingly.
    * If the scroll value is positive, it increments the current block index.
    * If the scroll value is negative, it decrements the current block index.
    * It wraps around the index if it goes out of bounds.
    * Param 1: ctx - The input action callback context.
    */
    private void OnScrollPerformed(InputAction.CallbackContext ctx)
    {
        float scroll = ctx.ReadValue<Vector2>().y;
        var matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null || matrix.rowsCount == 0) return;

        if (scroll > 0) 
            currentRow = (currentRow + 1) % matrix.rowsCount;
        else 
            currentRow = (currentRow - 1 + matrix.rowsCount) % matrix.rowsCount;

        currentColumn = Mathf.Clamp(currentColumn, 0, matrix.columnsCount - 1);

        int startCol = currentColumn;
        do
        {
            var bi = matrix.rows[currentRow].columns[currentColumn];
            if (bi != null && bi.Block != null)
                break;

            currentColumn = (currentColumn + 1) % matrix.columnsCount;
        }
        while (currentColumn != startCol);

        UpdateCurrentBlockFromMatrix();
        SetText();
        destroyPreviewBlock();
    }

    private void OnScrollUpPerformed(InputAction.CallbackContext ctx)
    {
        var matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null || matrix.rowsCount == 0) return;

        currentRow = (currentRow + 1) % matrix.rowsCount;
        currentColumn = Mathf.Clamp(currentColumn, 0, matrix.columnsCount - 1);

        int startCol = currentColumn;
        do
        {
            var bi = matrix.rows[currentRow].columns[currentColumn];
            if (bi != null && bi.Block != null) break;
            currentColumn = (currentColumn + 1) % matrix.columnsCount;
        }
        while (currentColumn != startCol);

        UpdateCurrentBlockFromMatrix();
        SetText();
        destroyPreviewBlock();
    }

    private void OnScrollDownPerformed(InputAction.CallbackContext ctx)
    {
        var matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null || matrix.rowsCount == 0) return;

        currentRow = (currentRow - 1 + matrix.rowsCount) % matrix.rowsCount;
        currentColumn = Mathf.Clamp(currentColumn, 0, matrix.columnsCount - 1);

        int startCol = currentColumn;
        do
        {
            var bi = matrix.rows[currentRow].columns[currentColumn];
            if (bi != null && bi.Block != null) break;
            currentColumn = (currentColumn + 1) % matrix.columnsCount;
        }
        while (currentColumn != startCol);

        UpdateCurrentBlockFromMatrix();
        SetText();
        destroyPreviewBlock();
    }

    private void ChangeColumn(int dir)
    {
        var matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null || matrix.columnsCount == 0) return;

        int start = currentColumn;
        do
        {
            currentColumn = (currentColumn + dir + matrix.columnsCount) % matrix.columnsCount;

            var bi = matrix.rows[currentRow].columns[currentColumn];
            if (bi != null && bi.Block != null)
                break;

        } while (currentColumn != start);

        UpdateCurrentBlockFromMatrix();
        SetText();
        destroyPreviewBlock();
    }
    /* OnRotatePerformed is called when the rotate action is performed.
    * It increments the rotation offset count to rotate the block being built.
    * The rotation offset count is used to determine the rotation of the block when it is placed in the world.
    * It wraps around the count if it exceeds the maximum value.
    * Param 1: ctx - The input action callback context.
    */
    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        rotationOffsetCount = (rotationOffsetCount + 1) % 4;
    }

    /* Sets the text for the current block name and updates the UI image.
    * If the current block is null, it sets the text to "No Block Selected" and clears the image.
    * If the current block is not null, it sets the text to the block name and updates the image with the block's sprite.
    */
    private void UpdateCurrentBlockFromMatrix()
    {
        var matrix = BlockInventoryManager.instance.inventoryMatrix;
        if (matrix == null) { currentBlock = null; return; }

        if (currentRow < 0 || currentRow >= matrix.rowsCount
         || currentColumn < 0 || currentColumn >= matrix.columnsCount)
        {
            currentBlock = null;
            return;
        }

        var bi = matrix.rows[currentRow].columns[currentColumn];
        currentBlock = bi != null ? bi.Block : null;
    }
    void SetText()
    {
        if (blockNameText != null && currentBlock != null)
        {
            int currentCount = BlockInventoryManager.instance.GetBlockCount(currentBlock);
            blockNameText.text = $"{currentBlock.BlockName} ({currentCount})\n";
            if (blockUIImage != null)
                blockUIImage.sprite = currentBlock.uiSprite;

            if (blockDescriptionText != null)
                blockDescriptionText.text = 
                    !string.IsNullOrEmpty(currentBlock.description)
                    ? currentBlock.description
                    : "";
        }
        else if (blockNameText != null)
        {
            blockNameText.text = "No Block Selected";
            if (blockUIImage != null)
                blockUIImage.sprite = null;
            if (blockDescriptionText != null)
                blockDescriptionText.text = "";
        }
    }

    // void ChangeCurrentBlock()
    // {
    //     float scroll = Input.mouseScrollDelta.y;
    //     if(scroll > 0)
    //     {
    //         currentBlockIndex++;
    //         if (currentBlockIndex > availableBuildingBlocks.Length - 1)
    //         {
    //             currentBlockIndex = 0;
    //         }
    //     } else if(scroll < 0)
    //     {
    //         currentBlockIndex--;
    //         if (currentBlockIndex < 0)
    //         {
    //             currentBlockIndex = availableBuildingBlocks.Length - 1;
    //         }
    //     }
    //     currentBlock = availableBuildingBlocks[currentBlockIndex];
    //     SetText();
    //     if (previewBlock != null)
    //     {
    //         Destroy(previewBlock);
    //         previewBlock = null;
    //     }
    // }
 
   
 
    /* Builds a block at the specified position in the world.
    * It checks if the current block is not null and if there are enough blocks in the inventory.
    * It performs a raycast to find a valid position to place the block.
    * If a valid position is found, it instantiates the block prefab and sets its position and rotation.
    * It also creates joints and connections with neighboring blocks if applicable.
    * Param 1: blockPrefab - The prefab of the block to be built.
    */
    void BuildBlock(GameObject blockPrefab)
    {
        if (ModeSwitcher.instance.currentMode != ModeSwitcher.Mode.Build) return;
        int count = BlockInventoryManager.instance.GetBlockCount(currentBlock);
        Debug.Log("build init");
        if (currentBlock == null || count <= 0) // No blocks remaining of this type
        {
            string popupMsg = "No blocks remaining of this type!";
            Debug.LogWarning(popupMsg);
            UIManager.Instance.ShowPopup(popupMsg, 2f);
            return;
        }
        LayerMask combinedMask = rayCastLayers & ~shieldLayer;
        LayerMask combinedMask_preview = combinedMask & ~previewIgnoreLayers; //NOTWORKING NEED FIX

        Ray ray = GetBuildRay();

        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, combinedMask_preview)) // Raycast hit something that is a block
        {
            if (AimingAtPickupHullCanPickUp(hitInfo)) return;
             Debug.Log("raycast hitting"+hitInfo.collider.gameObject.name);

            // 1. Instantiating the block

            if (hitInfo.collider.gameObject.layer == 6) // Hit a block
            {
                Vector3 localPoint = referenceTransform.InverseTransformPoint(hitInfo.point);
                Vector3 localNormal = referenceTransform.InverseTransformDirection(hitInfo.normal);
                float angleWithUp = Vector3.Angle(hitInfo.normal, referenceTransform.TransformDirection(Vector3.up));
                bool isTopSurface = (angleWithUp < 30f);
                bool isBottomSurface = (angleWithUp > 150f);
                bool isSideSurface = (!isTopSurface && !isBottomSurface);
                if (currentBlock.onlyInlinePlacement)
                {
                    Vector3 allowedDirection = referenceTransform.TransformDirection(currentBlock.attachDirection);
                    float alignmentAngle = Vector3.Angle(hitInfo.normal, allowedDirection);
                    float alignmentAngleNegative = Vector3.Angle(hitInfo.normal, -allowedDirection);
                    float tolerance = 20f;

                    if (alignmentAngle > tolerance && alignmentAngleNegative > tolerance)
                    {
                        string popupMsg = $"{currentBlock.BlockName} must be placed along the core direction!";
                        Debug.LogWarning(popupMsg);
                        UIManager.Instance.ShowPopup(popupMsg, 2f);
                        return;
                    }
                }
                if (isTopSurface && !currentBlock.isTopMountable)
                {
                    string popupMsg = "This block cannot be top-mounted!";
                    Debug.LogWarning(popupMsg);
                    UIManager.Instance.ShowPopup(popupMsg, 2f);
                    return;
                }
                if (isBottomSurface && !currentBlock.isBottomMountable)
                {
                    string popupMsg = "This block cannot be bottom-mounted!";
                    Debug.LogWarning(popupMsg);
                    UIManager.Instance.ShowPopup(popupMsg, 2f);
                    return;
                }
                if (isSideSurface && !currentBlock.isSideMountable)
                {
                    string popupMsg = "This block cannot be side-mounted!";
                    Debug.LogWarning(popupMsg);
                    UIManager.Instance.ShowPopup(popupMsg, 2f);
                    return;
                }
                Vector3 localSpawn = new Vector3( // Calculate spawn position
                    Mathf.RoundToInt(localPoint.x + localNormal.x / 2f),
                    Mathf.RoundToInt(localPoint.y + localNormal.y / 2f),
                    Mathf.RoundToInt(localPoint.z + localNormal.z / 2f)
                );
                Vector3Int spawnPosInt = Vector3Int.RoundToInt(localSpawn);
                Vector3 spawnWorldPos = referenceTransform.TransformPoint(localSpawn);
                Vector3 spawnInParentSpace = parent.InverseTransformPoint(spawnWorldPos);
                // Vector3 spawnInParentSpace = parent.InverseTransformPoint(commandModule.TransformPoint(localSpawn));
                GameObject newBlock = Instantiate(blockPrefab, parent); // Instantiate new block as child of command module

                // Play build sound
                audioSource.PlayOneShot(audioClip);

                newBlock.transform.localPosition = spawnInParentSpace; // Set local position of new block
                Quaternion newBlockWorldRotation = Quaternion.identity;
                if (isSideSurface)
                {
                    // 1. Convert World Normal to Local Normal (relative to the vehicle)
                    // This fixes the "toe-in" issue by ignoring World rotation
                    Vector3 localHitNormal = referenceTransform.InverseTransformDirection(hitInfo.normal);

                    // 2. Snap to the nearest LOCAL grid axis (e.g. Local Right, Local Forward)
                    Vector3 localSnappedNormal = new Vector3(
                        Mathf.Abs(localHitNormal.x) > 0.9f ? Mathf.Sign(localHitNormal.x) : 0f,
                        Mathf.Abs(localHitNormal.y) > 0.9f ? Mathf.Sign(localHitNormal.y) : 0f,
                        Mathf.Abs(localHitNormal.z) > 0.9f ? Mathf.Sign(localHitNormal.z) : 0f
                    );
                    localSnappedNormal.Normalize();

                    Quaternion targetLocalRotation = Quaternion.identity;

                    // 3. Determine Rotation Logic based on Connection Axis
                    bool isYAxisConnection = Mathf.Abs(currentBlock.attachDirection.y) > 0.9f;

                    if (isYAxisConnection && !currentBlock.isTopMountable)
                    {
                        // Rolling Axis: Align local Z with Vehicle's Local Forward (0,0,1)
                        Vector3 targetLocalZ = Vector3.forward;

                        // Connection Axis: Align local Y to point INTO the wall
                        // If connecting via Top (y=1), Top points into wall (-normal)
                        // If connecting via Bottom (y=-1), Bottom points into wall, so Top points OUT (+normal)
                        Vector3 targetLocalY = (currentBlock.attachDirection.y > 0) ? -localSnappedNormal : localSnappedNormal;

                        // Create rotation from these local vectors
                        targetLocalRotation = Quaternion.LookRotation(targetLocalZ, targetLocalY);
                    }
                    else
                    {
                        if (isYAxisConnection)
                        {
                            // LookRotation(forward, up):
                            // Set Forward (Z) to World Up.
                            // Set Up (Y) to point Into Wall.
                            targetLocalRotation = Quaternion.LookRotation(Vector3.up, localSnappedNormal);
                        }
                        else
                        {
                            // Standard logic for Back-connected blocks (Armor plates)
                            Quaternion toNormal = Quaternion.FromToRotation(currentBlock.attachDirection, -localSnappedNormal);
                            targetLocalRotation = toNormal;

                            // Upright Correction: Keep the block's "Up" aligned with Vehicle's "Up"
                            if (currentBlock.isSideMountable)
                            {
                                Vector3 projectedUp = Vector3.ProjectOnPlane(Vector3.up, localSnappedNormal).normalized;
                                if (projectedUp != Vector3.zero)
                                {
                                    targetLocalRotation = Quaternion.LookRotation(targetLocalRotation * Vector3.forward, projectedUp);
                                }
                            }
                        }
                    }

                    // 4. Convert back to World Rotation
                    newBlockWorldRotation = referenceTransform.rotation * targetLocalRotation;
                }

                else
                {
                    newBlockWorldRotation = referenceTransform.rotation;
                    if (isBottomSurface)
                    {
                        newBlockWorldRotation *= Quaternion.Euler(180f, 0f, 0f);
                    }
                }
                if (currentBlock.isRotatable) // If block is rotatable, apply rotation offset
                {
                    newBlockWorldRotation *= Quaternion.Euler(0f, 90f * rotationOffsetCount, 0f);
                }
                // newBlock.transform.localRotation *= Quaternion.Euler(0, 90f * rotationOffsetCount, 0);
                Quaternion finalLocalRotation = Quaternion.Inverse(parent.rotation) * newBlockWorldRotation;
                newBlock.transform.localRotation = finalLocalRotation;
                Rigidbody newBlockRb = newBlock.GetComponent<Rigidbody>();
                
                // Vector3Int spawnPosInt = Vector3Int.RoundToInt(localSpawn);
                // Debug.Log("Spawn grid coordinate: " + spawnPosInt);
                
                // 2. Creating joints and connections

                Hull newHull = newBlock.GetComponent<Hull>();
                if(newHull != null)
                {
                    newHull.sourceBlock = currentBlock; // Set the source block of the new block to the current selected block
                    foreach (Vector3Int offset in newHull.validConnectionOffsets) // Iterate over valid connection offsets of the new block as set in the block scriptable object
                    {
                        Vector3 offsetLocal = offset;
                        Vector3 offsetWorld = newBlock.transform.TransformDirection(offsetLocal);
                        Vector3 offsetInModule = referenceTransform.InverseTransformDirection(offsetWorld);
                        Vector3Int offsetInModuleInt = new Vector3Int(
                            Mathf.RoundToInt(offsetInModule.x),
                            Mathf.RoundToInt(offsetInModule.y),
                            Mathf.RoundToInt(offsetInModule.z)
                        );
                        Vector3Int neighborPos = spawnPosInt + offsetInModuleInt; // Calculate the position of the neighbor block
                        // Debug.Log("For block " + currentBlock.BlockName + "Checking neighbor at: " + neighborPos + " for connection offset: " + offset);
                        if (BlockManager.instance != null && BlockManager.instance.TryGetBlockAt(neighborPos, out Rigidbody neighborRb))
                        {
                            Hull neighborHull = neighborRb.GetComponent<Hull>();
                            if (neighborHull != null)
                            {
                                // gridOffset: direction from neighbor -> new block, in GRID / module space
                                Vector3Int gridOffset = spawnPosInt - neighborPos;
                                Vector3 gridOffsetVec = (Vector3)gridOffset;

                                // Rotation of neighbor in GRID space (same space as referenceTransform)
                                // worldRot = neighborRb.transform.rotation
                                // gridRot maps neighbor local -> grid
                                Quaternion neighborGridRot = Quaternion.Inverse(referenceTransform.rotation) * neighborRb.transform.rotation;

                                // Convert grid offset into neighbor-local offset
                                Vector3 neighborLocalOffset = Quaternion.Inverse(neighborGridRot) * gridOffsetVec;
                                Vector3Int neighborLocalOffsetInt = new Vector3Int(
                                    Mathf.RoundToInt(neighborLocalOffset.x),
                                    Mathf.RoundToInt(neighborLocalOffset.y),
                                    Mathf.RoundToInt(neighborLocalOffset.z)
                                );

                                // Now we¡¯re truly comparing in the neighbor¡¯s local grid
                                if (neighborHull.validConnectionOffsets.Contains(neighborLocalOffsetInt))
                                {
                                    var joint = newBlock.AddComponent<FixedJoint>();
                                    joint.connectedBody = neighborRb;
                                    joint.breakForce =
                                        newBlock.GetComponent<Hull>().sourceBlock.connectionStrength +
                                        neighborRb.GetComponent<Hull>().sourceBlock.connectionStrength;

                                    BlockManager.instance.AddConnection(spawnPosInt, neighborPos);
                                }
                            }
                        }
                        // else
                        // {
                        //     Debug.Log("No neighbor found at " + neighborPos + ".");
                        // }
                    }
                    BlockInventoryManager.instance.TryConsumeBlock(currentBlock, 1);
                    SetText();
                }
                // else
                // {
                //     Debug.LogWarning("New block has no Hull component. Skipping connection logic.");
                // }
                
                if (BlockManager.instance != null)
                {
                    // BlockManager.instance.AddBlock(spawnPosInt, newBlockRb); // Register the new block in the block manager
                    // Debug.Log("Registered new block at grid coordinate: " + spawnPosInt);
                }
            }
        }
    }


    /* Checks which direction thee joints should go.
    * It checks the x, y, and z coordinates of the neighbor position and the spawn position.
    * If the neighbor position is less than the spawn position in x, it returns true.
    * If the neighbor position is greater than the spawn position in x, it returns false.
    * If the neighbor position is less than the spawn position in y, it returns true.
    * If the neighbor position is greater than the spawn position in y, it returns false.
    * If the neighbor position is less than the spawn position in z, it returns true.
    * Otherwise, it returns false.
    * Param 1: neighborPos - The position of the neighbor block.
    * Param 2: spawnPos - The position of the block being built.
    * Returns true if the joint should connect, false otherwise.
    */
    bool ShouldConnect(Vector3Int neighborPos, Vector3Int spawnPos)
    {
        if(neighborPos.x < spawnPos.x) return true;
        if(neighborPos.x > spawnPos.x) return false;
        if(neighborPos.y < spawnPos.y) return true;
        if(neighborPos.y > spawnPos.y) return false;
        return neighborPos.z < spawnPos.z;
    }
 
    /* Destroys a block at the specified position in the world.
    * It performs a raycast to find the block to be destroyed.
    * If a block is hit, it checks if it is a valid block to destroy.
    * If it is, it adds the block to the inventory and destroys the block object.
    * It also removes the block from the block manager.
    */
    void DestroyBlock()
    {
        LayerMask combinedMask = rayCastLayers & ~shieldLayer;
        Ray ray = GetBuildRay();
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, combinedMask))
        {
            Debug.Log("Raycast hit: " + hitInfo.collider.gameObject.name);
            if (hitInfo.transform.CompareTag("Block") || ((hitInfo.transform.CompareTag("EnemyBlock") && hitInfo.transform.GetComponent<Hull>().canPickup)))
            {
                Hull hull = hitInfo.transform.GetComponent<Hull>();
                if (hull != null && hull.sourceBlock != null)
                {
                    BlockInventoryManager.instance.AddBlock(hull.sourceBlock, 1);
                    SetText();
                }
                Vector3 localPos = commandModule.InverseTransformPoint(hitInfo.transform.position);
                Vector3Int localPosInt = Vector3Int.RoundToInt(localPos);
                Destroy(hitInfo.transform.gameObject);
                if (BlockManager.instance != null)
                {
                    BlockManager.instance.RemoveBlock(localPosInt);
                }
            }
        }
    }
 
    // LEGACY, NO LONGER USED
    void HighlightBlock()
    {
        if (Physics.Raycast(shootingPoint.position, shootingPoint.forward, out RaycastHit hitInfo))
        {
            if (hitInfo.transform.tag == "Block")
            {
                if(lastHightlightedBlock == null)
                {
                    lastHightlightedBlock = hitInfo.transform.gameObject;
                    hitInfo.transform.gameObject.GetComponent<Renderer>().material.color = highlightedColor;
                }
                else if (lastHightlightedBlock != hitInfo.transform.gameObject)
                {
                    lastHightlightedBlock.GetComponent<Renderer>().material.color = normalColor;
                    hitInfo.transform.gameObject.GetComponent<Renderer>().material.color = highlightedColor;
                    lastHightlightedBlock = hitInfo.transform.gameObject;
                }
            }
            else if(lastHightlightedBlock != null)
            {
                lastHightlightedBlock.GetComponent<Renderer>().material.color = normalColor;
                lastHightlightedBlock = null;
            }
        }
    }

    /* Updates the preview of the block being built.
    * It performs a raycast to find a valid position to place the block.
    * If a valid position is found, it instantiates the preview block and sets its position and rotation.
    * It also applies the preview material to the block and sets it active.
    * If no valid position is found, it deactivates the preview block.
    * It also removes the physics components from the preview block to prevent it from falling.
    */
    void UpdatePreview()
    {
        if (ModeSwitcher.instance.currentMode != ModeSwitcher.Mode.Build) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        // We'll do the same conversion steps for the preview
        LayerMask effectiveMask = rayCastLayers & ~shieldLayer;
        effectiveMask = effectiveMask & ~previewIgnoreLayers;
        Ray ray = GetBuildRay();
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, effectiveMask))
        {
            if (AimingAtPickupHullCanPickUp(hitInfo)) {
                if (previewBlock != null) previewBlock.SetActive(false);
                return;
            }
            // Debug.Log("Raycast hit: " + hitInfo.collider.gameObject.name);
            // Debug.Log("Raycast hit object has parent: " + hitInfo.collider.gameObject.transform.parent?.gameObject.name);
            if (hitInfo.collider.gameObject.layer == 6 || (hitInfo.collider.gameObject.transform.parent != null && hitInfo.collider.gameObject.transform.parent.gameObject.layer == 6))
            {
                // Convert to commandModule space
                Vector3 localPoint = commandModule.InverseTransformPoint(hitInfo.point);
                Vector3 localNormal = commandModule.InverseTransformDirection(hitInfo.normal);

                // Round spawn
                Vector3 localSpawn = new Vector3(
                    Mathf.RoundToInt(localPoint.x + localNormal.x / 2f),
                    Mathf.RoundToInt(localPoint.y + localNormal.y / 2f),
                    Mathf.RoundToInt(localPoint.z + localNormal.z / 2f)
                );

                // If we haven't created the preview object yet, do it
                if (previewBlock == null)
                {
                    previewBlock = Instantiate(currentBlock.PreviewObject, parent);
                    previewBlockOriginalRotation = previewBlock.transform.rotation;
                    RemovePhysicsComponents(previewBlock);
                }

                // Convert to world, then parent-local
                Vector3 worldSpawn = commandModule.TransformPoint(localSpawn);
                Vector3 spawnInParentSpace = parent.InverseTransformPoint(worldSpawn);
                previewBlock.transform.localPosition = spawnInParentSpace;

                // Figure out orientation (top / bottom / side)
                float angleWithUp = Vector3.Angle(hitInfo.normal, commandModule.TransformDirection(Vector3.up));
                bool isTopSurface = angleWithUp < 30f;
                bool isBottomSurface = angleWithUp > 150f;
                bool isSideSurface = !isTopSurface && !isBottomSurface;

                // We'll build the final world rotation
                Quaternion newBlockWorldRotation = commandModule.rotation;

                if (isSideSurface)
                {
                    // === UPDATED LOCAL SPACE LOGIC ===

                    // 1. Convert World Normal to Local Normal (relative to the vehicle)
                    Vector3 localHitNormal = commandModule.InverseTransformDirection(hitInfo.normal);

                    // 2. Snap to the nearest LOCAL grid axis
                    Vector3 localSnappedNormal = new Vector3(
                        Mathf.Abs(localHitNormal.x) > 0.9f ? Mathf.Sign(localHitNormal.x) : 0f,
                        Mathf.Abs(localHitNormal.y) > 0.9f ? Mathf.Sign(localHitNormal.y) : 0f,
                        Mathf.Abs(localHitNormal.z) > 0.9f ? Mathf.Sign(localHitNormal.z) : 0f
                    );
                    localSnappedNormal.Normalize();

                    Quaternion targetLocalRotation = Quaternion.identity;

                    // 3. Determine Rotation Logic based on Connection Axis
                    bool isYAxisConnection = Mathf.Abs(currentBlock.attachDirection.y) > 0.9f;

                    if (isYAxisConnection && !currentBlock.isTopMountable)
                    {
                        Vector3 targetLocalZ = Vector3.forward; // Roll forward relative to car
                        Vector3 targetLocalY = (currentBlock.attachDirection.y > 0) ? -localSnappedNormal : localSnappedNormal; // Connect into wall
                        targetLocalRotation = Quaternion.LookRotation(targetLocalZ, targetLocalY);
                    }
                    else
                    {
                        if (isYAxisConnection)
                        {
                            targetLocalRotation = Quaternion.LookRotation(Vector3.up, localSnappedNormal);
                        }
                        else
                        {
                            Quaternion toNormal = Quaternion.FromToRotation(currentBlock.attachDirection, -localSnappedNormal);
                            targetLocalRotation = toNormal;

                            if (currentBlock.isSideMountable)
                            {
                                Vector3 projectedUp = Vector3.ProjectOnPlane(Vector3.up, localSnappedNormal).normalized;
                                if (projectedUp != Vector3.zero)
                                {
                                    targetLocalRotation = Quaternion.LookRotation(targetLocalRotation * Vector3.forward, projectedUp);
                                }
                            }
                        }
                    }

                    // 4. Convert back to World Rotation
                    newBlockWorldRotation = commandModule.rotation * targetLocalRotation;
                }
                else
                {
                    // top or bottom
                    if (isBottomSurface)
                    {
                        newBlockWorldRotation *= Quaternion.Euler(180f, 0f, 0f);
                    }
                }

                // Add user rotation offset
                if (currentBlock.isRotatable){ // If block is rotatable, apply rotation offset
                    newBlockWorldRotation *= Quaternion.Euler(0f, 90f * rotationOffsetCount, 0f);
                }

                // Convert world rotation -> parent local
                Quaternion finalLocalRotation = Quaternion.Inverse(parent.rotation) * newBlockWorldRotation;
                previewBlock.transform.localRotation = finalLocalRotation;
                previewBlock.SetActive(true);
            }
            else
            {
                if (previewBlock != null)
                {
                    previewBlock.SetActive(false);
                }
            }
        }
        else
        {
            if (previewBlock != null)
            {
                previewBlock.SetActive(false);
            }
        }
    }

    /* Removes the physics components from the preview block.
    * It iterates through all the colliders and fixed joints in the block and destroys them.
    * It also destroys the rigidbody component if it exists.
    * Param 1: obj - The game object to remove the physics components from.
    */
    void RemovePhysicsComponents(GameObject obj)
    {
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
        {
            Destroy(col);
        }
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
        foreach (FixedJoint joint in obj.GetComponents<FixedJoint>())
        {
            Destroy(joint);
        }
    }

    /* Applies the preview material to the block.
    * It iterates through all the renderers in the block and sets their material to the preview material.
    * Param 1: obj - The game object to apply the preview material to.
    */
    void ApplyPreviewMaterial(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = previewMaterial;
        }
    }

    /* Reparents all blocks to the reference transform.
    * It iterates through all the children of the parent transform and sets their parent to the reference transform.
    */
    private void ReparentAllBlocksToReference()
    {
        List<Transform> blocksToMove = new List<Transform>();
        foreach (Transform child in parent)
        {
            blocksToMove.Add(child);
        }
        foreach (Transform child in blocksToMove)
        {
            child.SetParent(referenceTransform, true);
        }
    }

    /* Transfers all blocks from the reference transform to the parent transform.
    * It iterates through all the children of the reference transform and sets their parent to the parent transform.
    */
    private void TransferBlocksToParent()
    {
        List<Transform> blocksToTransfer = new List<Transform>();
        foreach (Transform child in referenceTransform)
        {
            blocksToTransfer.Add(child);
        }
        foreach (Transform child in blocksToTransfer)
        {
            child.SetParent(parent, true);
        }
    }
    public void RefreshSelection()
    {
        UpdateCurrentBlockFromMatrix();
        SetText();
    }
    bool AimingAtPickupHullCanPickUp(RaycastHit hit)
    {
        Hull h = hit.collider.GetComponentInParent<Hull>();
        return h != null && h.canPickup;
    }
}

// [System.Serializable]
// public class BlockInventory
// {
//     public Block Block;
//     public int CurrentCount;
//     public int MaxStack = 99;
// }