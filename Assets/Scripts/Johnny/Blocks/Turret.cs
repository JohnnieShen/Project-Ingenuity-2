using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;
using System.Collections.Generic;

/**
    * Turret class is responsible for handling the turret's aiming, shooting, and reloading.
    * It works by using the MultiAimConstraint component to aim at the target.
    */
public class Turret : MonoBehaviour
{
    /* 
    * Author: Johnny
    * Summary: This script is attached to a turret block in the game. It manages the turret's aiming, shooting, and reloading.
    * The turret uses the MultiAimConstraint component to aim at a target and can shoot projectiles at a specified fire rate.
    * The turret can also check for obstacles in its line of sight and block the shot if necessary.
    * The turret can be configured to use either ballistic or energy projectiles, and it can be set to AI mode for enemy turrets.
    */

    // List of MultiAimConstraint components that will be used to aim at the target.
    // Each represents a MultiAimConstraint under the Rig component under turret, there should be multiple.
    // We are only accessing it here to turn on and off aiming (by setting the weight of the target).
    [SerializeField] private List<MultiAimConstraint> aimConstraints = new List<MultiAimConstraint>();

    // The target that the turret will aim at, this will be passed into the MultiAimConstraint components to use with inverse kinematics.
    private Transform aimTarget;
    [Header("Shooting Settings")]
    [SerializeField] private Transform shootPoint;
    private GameObject projectilePrefab;
    [SerializeField] private GameObject playerProjectilePrefab;
    [SerializeField] private GameObject enemyProjectilePrefab;
    [SerializeField] private float fireRate = 1f;
    // [SerializeField] private float reloadTime = 2f;
    [SerializeField] private float ballisticDamage = 10f;
    [SerializeField] private float energyDamage = 10f;
    [SerializeField] private int magazineSize = 5;
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem muzzleSpark;
    [Header("Obstacle Check Settings")]
    [Header("Audio")]
    [SerializeField] private AudioSource gunAudioSource;
    [SerializeField] private AudioClip gunShotClip;
    [SerializeField] private float checkDistance = 100f;
    public LineRenderer blockedLine;

    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    public bool isBlocked = false;
    public bool isAI = false;
    public bool isEnergy;
    public int ammoCost;

    [SerializeField] private float aiSpreadAngle = 2f;
    // public Transform aimTransform;

    /**
    * Start is called before the first frame update.
    * It initializes the aim target and sets up the MultiAimConstraint components to aim at it.
    * It also sets up the blocked line renderer and initializes the ammo count.
    */
    void Start()
    {
        // aimTarget = FreeCameraLook.instance?.aimTarget;

        if (gameObject.GetComponent<Hull>() != null)
        {
            if (gameObject.GetComponent<Hull>().canPickup)
                return;
        }
        EnemyAI enemyAI = transform.parent.GetComponentInChildren<EnemyAI>();
         if (enemyAI != null && enemyAI.aimTransform != null)
        {
            // For every AI, we have a aimTransform under the top parent, so we get that here proramatically.
            aimTarget = enemyAI.aimTransform;
            isAI = true;
        }
        else
        {
            // If it's a player, there's a aimTarget under FreeCameraLook so find it there.
            aimTarget = FreeCameraLook.instance?.aimTarget;
            isAI = false;
        }
        if (aimTarget == null)
        {
            Debug.LogWarning("Turret target not assigned in FreeCameraLook or FreeCameraLook instance is missing.");
            return;
        }

        foreach (MultiAimConstraint constraint in aimConstraints)
        {
            //For each MultiAimConstraint, set the target to aimTarget.
            SetConstraintTarget(constraint, aimTarget);
        }
        // Rebuild the Rig because we changed its parameters.
        RigBuilder rigs = GetComponent<RigBuilder>();
        if (rigs != null)
            rigs.Build();

        currentAmmo = magazineSize;
        if (blockedLine != null)
        {
            blockedLine.startWidth = 0.05f;
            blockedLine.endWidth = 0.05f;
            blockedLine.enabled = false;
        }

        // Choose bullet prefab for player or AI
        projectilePrefab = isAI ? enemyProjectilePrefab : playerProjectilePrefab;
    }
    
    /* Disable aiming constraints by setting their weight to 0.
    * This is used when the turret is not connected to the core
    */
    private void DisableAimConstraints()
    {
        foreach (MultiAimConstraint constraint in aimConstraints)
        {
            SetConstraintWeight(constraint, 0f);
        }
        RigBuilder rigs = GetComponent<RigBuilder>();
        if (rigs != null)
            rigs.Build();
    }

    /* Set the weight of the MultiAimConstraint to the specified value.
    * This is used to enable or disable aiming for the turret.
    * Param1: constraint - The MultiAimConstraint to set the weight for.
    * Param2: weight - The weight to set for the constraint.
    */
    private void SetConstraintWeight(MultiAimConstraint constraint, float weight)
    {
        if (constraint != null)
            constraint.weight = weight;
    }

    /* Update is called once per frame.
    * It checks if the turret is blocked by an obstacle and updates the blocked line renderer if necessary.
    * It also disables the aim constraints if the turret is not connected to the core.
    */
    void Update()
    {
        Hull hull = GetComponent<Hull>();
        if (hull != null && hull.canPickup)
        {
            if (blockedLine != null)
                blockedLine.enabled = false;
            DisableAimConstraints(); // Disable aiming when the turret's canPickUp is true, aka it is not connected to the core.
            return;
        }
        if (aimTarget == null)
            return;
        CheckIfBlocked();
    }

    /* * OnEnable is called when the script is enabled.
    * It subscribes to the OnFire event from the FreeCameraLook instance to handle firing the turret.
    * It also checks if the turret is AI and does not subscribe to the event if it is. Because AI firing is handle by EnemyBehavior.
    */
    void OnEnable()
    {
        if (isAI)
            return;
        FreeCameraLook.OnFire += HandleFireEvent;
    }

    /* OnDisable is called when the script is disabled.
    * It unsubscribes from the OnFire event from the FreeCameraLook instance to prevent memory leaks.
    * It also checks if the turret is AI and does not unsubscribe if it is. Because AI firing is handle by EnemyBehavior.
    */
    void OnDisable()
    {
        if (isAI)
            return;
        FreeCameraLook.OnFire -= HandleFireEvent;
    }

    // Call this if you want to fire the turret from another script.
    // Before calling, ensure that the turret script is well configured and the aimTarget is either set or can be found.
    public void HandleFireEvent()
    {
        if (!this.enabled) 
            return;
        Hull hull = GetComponent<Hull>();
        if (hull != null && hull.canPickup) // If the turret is not connected to the core, don't fire.
            return;
        if (isReloading) // If the turret is reloading, don't fire.
            return;

        if (VehicleResourceManager.Instance != null)
        {
            if (isEnergy)
            {
                if (VehicleResourceManager.Instance.energyAmmoCount < ammoCost)
                {
                    Debug.Log("Not enough energy ammo");
                    return;
                }
            }
            else // Ballistic turret.
            {
                if (VehicleResourceManager.Instance.ballisticAmmoCount < ammoCost)
                {
                    Debug.Log("Not enough ballistic ammo");
                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning("VehicleResourceManager instance not found");
            return;
        }
        if (isBlocked) // If the turret is blocked, don't fire.
        {
            return;
        }

        if (Time.time >= nextFireTime) // If the turret is ready to fire, shoot, then wait for the next fire time.
        {
            Shoot();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }

    // Fire the turret, instantiate a projectile and set its damage.
    // This is private because we don't want other scripts to access this, use the wrapper function HandleFireEvent instead.
    private void Shoot()
    {
        if (projectilePrefab != null && shootPoint != null)
        {
            Quaternion projRotation = shootPoint.rotation;

            if (isAI && aiSpreadAngle > 0f)
            {
                projRotation *= Quaternion.Euler(
                    Random.Range(-aiSpreadAngle, aiSpreadAngle),
                    Random.Range(-aiSpreadAngle, aiSpreadAngle),
                    0f);
            }
            GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, projRotation);
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
            {
                projScript.SetDamage(ballisticDamage, energyDamage);
            }
            if (!isAI) VehicleResourceManager.Instance.OnTurretFired(isEnergy, ammoCost);

            // Bullet inherits velocity of turret block
            Vector3 inheritedVelocity = this.GetComponent<Rigidbody>().linearVelocity;
            projScript.velocity = inheritedVelocity + projectile.transform.forward * projScript.speed;
        }
        else
        {
            Debug.LogWarning("Projectile prefab or shoot point is not assigned.");
        }
        if (muzzleFlash != null)
        {
            muzzleFlash.transform.position = shootPoint.position;
            muzzleFlash.transform.rotation = shootPoint.rotation;
            muzzleFlash.Play();
        }
        if (muzzleSpark != null)
        {
            muzzleSpark.transform.position = shootPoint.position;
            muzzleSpark.transform.rotation = shootPoint.rotation;
            muzzleSpark.Play();
        }
        //Debug.Log("Shoot called");
        if (gunAudioSource != null && gunShotClip != null) {
            gunAudioSource.PlayOneShot(gunShotClip);
        }
        // currentAmmo--;
    }

    // Coroutine to reload the turret, set isReloading to true, wait for reloadTime, then set currentAmmo to magazineSize and isReloading to false.
    // private IEnumerator Reload()
    // {
    //     isReloading = true;
    //     Debug.Log("Reloading...");
    //     yield return new WaitForSeconds(reloadTime);
    //     currentAmmo = magazineSize;
    //     isReloading = false;
    //     Debug.Log("Reload complete.");
    // }

    // Set the target of the MultiAimConstraint to the new target.
    // This is used to change the target of the turret when the player changes the aim target.
    // Param1: constraint - The MultiAimConstraint to set the target for.
    // Param2: newTarget - The new target to set for the constraint.
    private void SetConstraintTarget(MultiAimConstraint constraint, Transform newTarget)
    {
        if (constraint == null || newTarget == null)
            return;

        var data = constraint.data;
        var sources = data.sourceObjects;

        if (sources.Count == 0)
        {
            sources.Add(new WeightedTransform(newTarget, 1f)); // If this is the only target, add it and set the weight to 1.
        }
        else
        {
            sources.SetTransform(0, newTarget); // If there are multiple targets, set the first target to the new target.
            // If in the case of multiple targets set up before running, the first target is the one that will be overwritten and set so be careful.
        }

        constraint.weight = 1f;
        data.sourceObjects = sources;
        constraint.data = data;
    }

    // Check if the turret is blocked by an obstacle.
    private void CheckIfBlocked()
    {
        if (shootPoint == null) 
        {
            isBlocked = false;
            return;
        }

        Ray ray = new Ray(shootPoint.position, shootPoint.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, checkDistance))
        {
            // If the turret is not AI, check if the object hit is a block or core, if it is, set isBlocked to true.
            if (!isAI) {
                // Debug.Log("Hit object: " + hit.collider.name);
                Debug.DrawRay(ray.origin, ray.direction * checkDistance, Color.red);
                if (hit.collider.CompareTag("Block")||hit.collider.CompareTag("Core"))
                {
                    if (blockedLine != null)
                    {
                        blockedLine.enabled = true;
                        blockedLine.positionCount = 2;
                        blockedLine.SetPosition(0, shootPoint.position);
                        blockedLine.SetPosition(1, hit.point);
                    }
                    isBlocked = true;
                    return;
                }
            }
            else
            {
                // If the turret is AI, check if the object hit is an enemy block, if it is, set isBlocked to true.
                if (hit.collider.CompareTag("EnemyBlock"))
                {
                    isBlocked = true;
                    return;
                }
            }
        }
        // If the raycast doesn't hit anything, set isBlocked to false.
        isBlocked = false;
        if (blockedLine != null)
            blockedLine.enabled = false;
    }

    /* Set the aim accuracy for AI turrets.
    * This is used to set the spread angle for AI turrets to make them less accurate.
    * Param1: spread - The spread angle to set for the AI turret.
    */
    public void SetAISpread(float spread) => aiSpreadAngle = Mathf.Max(0f, spread);
}
