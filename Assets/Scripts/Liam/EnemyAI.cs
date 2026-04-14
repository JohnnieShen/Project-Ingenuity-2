using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.ProBuilder.MeshOperations;

public class EnemyAI : MonoBehaviour
{
    /* 
    * Author: Johnny
    * Summary: This script manages the AI behavior of an enemy vehicle in the game.
    * It is based on a finite state machine that handles different states such as Patrol, Chase, Attack, and Flee.
    * The AI uses raycasting to detect the player and determine line of sight.
    * It also manages the enemy's movement and turret firing behavior.
    */
    public enum AIState { Patrol, Chase, Attack, Flee }
    public AIState currentState = AIState.Patrol;

    [Header("AI Settings")]
    public float detectionRange = 30f;
    public float attackRange = 15f;
    public float patrolRange = 20f;
    public float stateUpdateInterval = 0.5f;
    public float patrolPointWaitTime = 2f;
    public float fleeHealthThreshold = 0.3f;
    public float patrolTimeout = 10f;

    [Header("References")]
    public Transform playerTarget;
    public EnemyMovement enemyMovement;
    public List<Turret> enemyTurrets = new List<Turret>();

    public void RegisterTurret(Turret turret)
    {
        if (!enemyTurrets.Contains(turret))
        {
            enemyTurrets.Add(turret);
            turret.SetAISpread(aiSpreadAngle);
        }
    }

    public void UnregisterTurret(Turret turret)
    {
        if (enemyTurrets.Contains(turret))
        {
            enemyTurrets.Remove(turret);
        }
    }

    public HealthSystem healthSystem;

    private Vector3 patrolCenter;
    private float stateUpdateTimer;
    [SerializeField] private float patrolWaitTimer;
    private bool hasLineOfSight;
    private float patrolTimer;
    private GameObject patrolTargetObject;
    public LayerMask enemyLayer;
    public LayerMask shieldLayer;
    public Transform aimTransform;
    public float aimDispersionMultiplier = 1f;
    public float aiSpreadAngle = 2f;

    /* Start is called before the first frame update.
    * It initializes the AI by setting the player target, registering the vehicle structure, and initializing the AI state.
    * It also sets the patrol center and initializes the health system.
    * It sets the maximum total health and initializes the enemy turrets.
    * It also sets the AI spread angle for the turrets.
    */

    // void OnEnable()
    // {
    //     Debug.Log($"[{name}] EnemyAI enabled");
    // }

    // void OnDisable()
    // {
    //     Debug.LogWarning($"[{name}] EnemyAI disabled");
    // }

    void Start()
    {
        if (playerTarget == null)
        {
            GameObject coreObject = GameObject.FindWithTag("Core");
            if (coreObject != null)
            {
                playerTarget = coreObject.transform;
            }
            else
            {
                Debug.LogWarning("No Core found");
            }
        }
        if (!EnemyBlockManager.instance.vehicles.ContainsKey(this))
        {
            InitializeVehicleStructure();
        }
        patrolCenter = transform.position;
        if (healthSystem == null) {
            healthSystem = GetComponent<HealthSystem>();
        }
        InitializeAI();
        // if (enemyTurrets.Count == 0)
        //     enemyTurrets.AddRange(GetComponentsInChildren<Turret>());
        // foreach (Turret t in enemyTurrets)
        //     if (t != null) t.SetAISpread(aiSpreadAngle);
        

        //TODO: Delay calculating until ai structure is initialized?
    }

    /* InitializeAI is called to set up the AI's initial state and target position.
    * It registers the health system's OnHealthChanged event to handle health changes.
    * It also sets the target position for the enemy movement and initializes the patrol point.
    * It sets the target position to a new GameObject if it is not already set.
    * It also sets a random patrol point for the AI to move towards.
    */
    void InitializeAI()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged.AddListener(HandleHealthChanged);
        }
        if (enemyMovement.targetPosition == null)
        {
            patrolTargetObject = new GameObject("EnemyNavigationTarget");
            enemyMovement.targetPosition = patrolTargetObject.transform;
        }
        SetRandomPatrolPoint();
    }

    /* OnDestroy is called when the MonoBehaviour will be destroyed.
    * It unregisters the health system's OnHealthChanged event to prevent memory leaks.
    * It also resets the ballistic ammo count in the VehicleResourceManager and updates the UI.
    * It also handles any necessary cleanup for the enemy AI before destruction.
    */
    void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged.RemoveListener(HandleHealthChanged);
        if (VehicleResourceManager.Instance != null)
        {
            VehicleResourceManager.Instance.ballisticAmmoCount = VehicleResourceManager.Instance.maxBallisticAmmo;
            VehicleResourceManager.Instance.UpdateBallisticAmmoUI();
        }
    }

    /* Update is called once per frame.
    * It updates the state machine timer and checks if it is time to update the AI state.
    * It also handles the current state of the AI by calling the appropriate behavior method.
    * It updates the line of sight to the player and handles the AI's behavior based on the current state.
    */
    void Update()
    {
        stateUpdateTimer -= Time.deltaTime;
        if (stateUpdateTimer <= 0)
        {
            stateUpdateTimer = stateUpdateInterval;
            UpdateStateMachine();
        }

        HandleCurrentState();
    }

    /* UpdateStateMachine is called to update the AI's state based on the player's position and line of sight.
    * It checks the distance to the player and updates the line of sight.
    * It then switches between different states (Patrol, Chase, Attack, Flee) based on the distance and line of sight.
    * It handles the transition between states and updates the AI's behavior accordingly.
    */
    void UpdateStateMachine()
    {
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        UpdateLineOfSight();

        switch (currentState)
        {
            case AIState.Patrol:
                if (distanceToPlayer <= detectionRange && hasLineOfSight)
                    currentState = AIState.Chase;
                break;

            case AIState.Chase:
                if (distanceToPlayer <= attackRange && hasLineOfSight)
                    currentState = AIState.Attack;
                else if (distanceToPlayer > detectionRange || !hasLineOfSight)
                    currentState = AIState.Patrol;
                break;

            case AIState.Attack:
                if (enemyTurrets.Count == 0)
                    currentState = AIState.Flee;
                else if (distanceToPlayer > attackRange || !hasLineOfSight)
                    currentState = AIState.Chase;
                break;

            case AIState.Flee:
                if (healthSystem.GetHealthPercentage() > fleeHealthThreshold && enemyTurrets.Count > 0)
                    currentState = AIState.Patrol;
                break;
        }
    }

    /* HandleCurrentState is called to handle the current state of the AI.
    * It switches between different states (Patrol, Chase, Attack, Flee) and calls the appropriate behavior method for each state.
    * It handles the AI's movement and behavior based on the current state.
    * It also handles the AI's turret firing behavior and updates the target position for the enemy movement.
    */
    void HandleCurrentState()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolBehavior();
                break;

            case AIState.Chase:
                ChaseBehavior();
                break;

            case AIState.Attack:
                AttackBehavior();
                break;

            case AIState.Flee:
                FleeBehavior();
                break;
        }
    }

    /* PatrolBehavior is called to handle the patrol behavior of the AI.
    * It increments the patrol timer and checks if the AI is within the arrival distance of the target position or if the patrol timer has exceeded the patrol timeout.
    * If so, it increments the patrol wait timer and checks if it has timed out.
    * If it has, it sets a new random patrol point and resets the patrol wait timer and patrol timer.
    */
    void PatrolBehavior()
    {
        patrolTimer += Time.deltaTime; // Increment the patrol timer.
        if (Vector3.Distance(transform.position, enemyMovement.targetPosition.position) < enemyMovement.arrivalDistance || 
        patrolTimer >= patrolTimeout) // If we are within the arrival distance of the target position or the patrol timer has exceeded the patrol timeout.
        {
            patrolWaitTimer += Time.deltaTime; // Increment the patrol wait timer.
            
            if (patrolWaitTimer >= patrolPointWaitTime) // If we have waited until the patrol wait timer times out
            {
                SetRandomPatrolPoint(); // Set a new random patrol point.
                patrolWaitTimer = 0;
                patrolTimer = 0;
            }
        }
    }

    /* ChaseBehavior is called to handle the chase behavior of the AI.
    * It sets the target position for the enemy movement to the player's position.
    */
    void ChaseBehavior()
    {
        enemyMovement.targetPosition.position = playerTarget.position; // Set the target position to the player's position.
    }

    /* AttackBehavior is called to handle the attack behavior of the AI.
    * It sets the aim transform position to the player's position and calls the HandleFireEvent method for each turret that is active and not blocked.
    * It handles the turret firing behavior and updates the aim transform for the AI.
    */
    void AttackBehavior()
    {
        if (playerTarget != null && aimTransform != null) // If the player target and aim transform of this AI are not null.
        {
            aimTransform.position = playerTarget.position;
        }

        // enemyMovement.targetPosition.position = playerTarget.position;

        foreach (Turret t in enemyTurrets)
            if (t == null)
                enemyTurrets.Remove(t);
            else if (t.isActiveAndEnabled && !t.isBlocked)
                t.HandleFireEvent();
    }

    /* FleeBehavior is called to handle the flee behavior of the AI.
    * It sets the target position to the opposite direction of the player and moves away from the player.
    * It handles the AI's movement and behavior when it is low on health.
    */
    void FleeBehavior()
    {
        // Set the target position to the opposite direction of the player.
        if (transform == null || playerTarget == null) return;
        Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
        enemyMovement.targetPosition.position = transform.position + fleeDirection * detectionRange;
    }

    /* SetRandomPatrolPoint is called to set a random patrol point for the AI.
    * It generates a random point within the patrol range and sets the target position for the enemy movement to that point.
    * It creates a new GameObject to represent the target position if it is not already set.
    * It sets the target position to the new target point.
    */
    void SetRandomPatrolPoint()
    {
        // Set a random point within the patrol range.
        Vector2 randomPoint = Random.insideUnitCircle * patrolRange;
        Vector3 newTarget = patrolCenter + new Vector3(randomPoint.x, 0, randomPoint.y);

        if (enemyMovement.targetPosition == null)
        {
            GameObject tempTarget = new GameObject("PatrolTarget");
            enemyMovement.targetPosition = tempTarget.transform;
        }
        // Set the target position to the new target.
        enemyMovement.targetPosition.position = newTarget;
    }

    // Update the line of sight of the AI to the player.
    // This method uses raycasting to check if the AI can see the player.
    // It ignores the enemy layer to prevent hitting the enemy's own blocks.
    // It sets the hasLineOfSight variable to true if the raycast hits a Block or Core, and false otherwise.
    // It also draws a debug line to visualize the raycast and its hit point.
    void UpdateLineOfSight()
    {
        // if (playerTarget == null)
        // {
        //     Debug.LogWarning("Player target is null, cannot compute line of sight.");
        //     return;
        // }

        Vector3 direction = (playerTarget.position - transform.position).normalized; // Calculate the direction to the player.
        LayerMask combinedIgnore = enemyLayer | shieldLayer;
        LayerMask mask = ~combinedIgnore;
        // Debug.Log("Raycasting from " + transform.position + " towards " + playerTarget.position + " with direction " + direction);
        RaycastHit hit;
        bool hitDetected = Physics.Raycast(transform.position, direction, out hit, detectionRange, mask);

        if (hitDetected) // If the raycast hit an object.
        {
            // Debug.Log("Raycast hit: " + hit.collider.name + " (Tag: " + hit.collider.tag + ") at distance: " + hit.distance);
            hasLineOfSight = hit.collider.CompareTag("Block") || hit.collider.CompareTag("Core") || (hit.collider.CompareTag("ConnectionPoint") && (hit.collider.transform.parent.parent.CompareTag("Block")||hit.collider.transform.parent.parent.CompareTag("Core"))); // Check if the hit object has the required tag (Block/Core).

            if (!hasLineOfSight)
                // Debug.Log("Hit object does not have the required tag (Block/Core).");

                Debug.DrawLine(transform.position, hit.point, hasLineOfSight ? Color.green : Color.red);
        }
        else
        {
            // Debug.Log("Raycast did not hit any object within detection range.");
            hasLineOfSight = false;
            Debug.DrawRay(transform.position, direction * detectionRange, Color.gray);
        }
    }

    /* HandleHealthChanged is called when the health of the AI changes.
    * It checks if the current health percentage is below the flee health threshold.
    * If so, it changes the AI state to Flee.
    * It handles the AI's behavior when it is low on health.
    * It also updates the AI's state based on the current health percentage.
    */
    void HandleHealthChanged(float currentTotalHealth)
    {
        if (healthSystem.GetHealthPercentage() <= fleeHealthThreshold 
            && currentState != AIState.Flee)
        {
            currentState = AIState.Flee;
        }
    }

    /* InitializeVehicleStructure is called to initialize the vehicle structure of the AI.
    * It registers the blocks of the vehicle with the EnemyBlockManager and builds the connection graph.
    * It gets all the Hull components in the vehicle and registers them with the EnemyBlockManager.
    * It also builds the connection graph for the vehicle.
    * It handles the initialization of the vehicle structure and its blocks.
    */
    public void InitializeVehicleStructure()
    {
        Hull[] blocks = transform.parent.GetComponentsInChildren<Hull>();
        foreach (Hull block in blocks)
        {
            //Debug.Log("Registering block: " + block.name);
            Vector3Int localPos = Vector3Int.RoundToInt(
                transform.InverseTransformPoint(block.transform.position)
            );
            //Debug.Log("Local position: " + localPos);
            EnemyBlockManager.instance.RegisterBlock(this, localPos, block.GetComponent<Rigidbody>());
        }
        BuildConnectionGraph();
        StartCoroutine(DelayedInitMaxHealth());
        // if (healthSystem == null)
        //     healthSystem = GetComponent<HealthSystem>();
        // float initialMax = healthSystem.CalculateTotalHealth();
        // healthSystem.InitializeMaxHealth(initialMax);
    }
    private IEnumerator DelayedInitMaxHealth()
    {
        yield return new WaitForSeconds(0.5f);

        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();

        float initialMax = healthSystem.CalculateTotalHealth();
        healthSystem.InitializeMaxHealth(initialMax);
    }
    /* BuildConnectionGraph is called to build the connection graph for the vehicle.
    * It clears the existing block connections and iterates through all the blocks in the vehicle.
    * It gets the FixedJoint components of each block and checks if they are connected to another block.
    * It adds the connections to the EnemyBlockManager and builds the connection graph for the vehicle.
    * It handles the connections between blocks and their positions in the vehicle structure.
    */
    public void BuildConnectionGraph()
    {
        EnemyBlockManager.instance.vehicles[this].blockConnections.Clear();
        foreach (var blockEntry in EnemyBlockManager.instance.GetBlocksForVehicle(this))
        {
            Rigidbody rb = blockEntry.Value;
            FixedJoint[] joints = rb.GetComponents<FixedJoint>();

            foreach (FixedJoint joint in joints)
            {
                // Debug.Log("Joint from" + rb.name + " to " + joint.connectedBody.name);
                if (joint.connectedBody != null)
                {
                    Vector3Int connectedPos = Vector3Int.RoundToInt(
                        transform.InverseTransformPoint(joint.connectedBody.transform.position)
                    );
                    // Debug.Log(("added connection from " + blockEntry.Key + " to " + connectedPos));
                    EnemyBlockManager.instance.AddConnection(this, blockEntry.Key, connectedPos);
                }
            }
        }
    }

    /* OnDrawGizmosSelected is called when the object is selected in the editor.
    * It draws gizmos to visualize the patrol range, detection range, and attack range of the AI.
    * It also draws a wire sphere to represent the patrol center and the target position for the enemy movement.
    * It handles the visualization of the AI's behavior and its ranges in the editor.
    */
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(patrolCenter, patrolRange);
        Gizmos.color = Color.green;
        if (enemyMovement?.targetPosition != null)
            Gizmos.DrawSphere(enemyMovement.targetPosition.position, 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}