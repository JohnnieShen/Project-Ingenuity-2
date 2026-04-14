using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ConvertToAI : MonoBehaviour
{
    /*
    Author: Liam
    Summary: This script can be used by developers to create prefabs for enemy AI vehicles. Once a car has been
    built, this script can be attached to the block parent in the scene tree while still in build mode. This will
    perfom all of the modifications to the game object necessary for it to be saved as an AI prefab. Then, the
    script destroys itself. The prefab can then be spawned using the enemy spawner script.
    */

    /*
    Start runs automatically to convert the vehicle as soon as the script component is added to block parent.
    */
    private void Start()
    {
        // Pause game so blocks do not move
        Time.timeScale = 0;

        // Command module is a child of block parent
        var commandModule = transform.Find("CommandModule");

        // Lists of blocks are stored in three lists
        List<Transform> children = new List<Transform>();
        List<Wheel> wheels = new List<Wheel>();
        List<Turret> turrets = new List<Turret>();

        foreach (Transform child in transform)
        {
            // Used to track children so they can be reparented
            children.Add(child);

            // Convert blocks from Player tag and layer to Enemy tag and layer
            child.tag = "EnemyBlock";
            // child.gameObject.layer = LayerMask.NameToLayer("EnemyBlock");

            // Obtain reference to wheel
            Wheel wheel = child.GetComponentInChildren<Wheel>();
            if (wheel != null)
            {
                wheel.isAI = true;
                wheels.Add(wheel);
            }

            // Obtain reference to turret
            Turret turret = child.GetComponent<Turret>();
            if (turret != null)
            {
                turret.isAI = true;
                turrets.Add(turret);
            }

            // Obtain reference to shield generator
            ShieldGenerator shield = child.GetComponent<ShieldGenerator>();
            if (shield != null)
            {
                shield.SetAI(true);
            }
        }
        int enemyBlockLayer = LayerMask.NameToLayer("EnemyBlock");
        int shieldLayerIndex = LayerMask.NameToLayer("Shield");
        foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = enemyBlockLayer;
        }
        // Set up HealthSystem script
        HealthSystem healthSystem = commandModule.AddComponent<HealthSystem>();

        // Set up EnemyMovement script
        EnemyMovement enemyMovement = commandModule.AddComponent<EnemyMovement>();
        enemyMovement.wheels = wheels;
        enemyMovement.rb = commandModule.GetComponent<Rigidbody>();

        // Set up EnemyAI script
        EnemyAI enemyAI = commandModule.AddComponent<EnemyAI>();
        Transform aimTransform = new GameObject("AimTarget").transform;
        aimTransform.parent = transform;
        enemyAI.aimTransform = aimTransform;
        enemyAI.enemyTurrets = turrets;
        enemyAI.enemyMovement = enemyMovement;
        enemyAI.healthSystem = healthSystem;
        // int enemyBlockLayer = LayerMask.NameToLayer("EnemyBlock");
        enemyAI.enemyLayer = (1 << enemyBlockLayer) | (1 << shieldLayerIndex);

        // Remove AimSphere and VehicleResourceManager scripts which are only used by the player vehicle
        Destroy(commandModule.GetComponent<AimSphere>());
        Destroy(commandModule.GetComponent<VehicleResourceManager>());

        // Reset transforms
        Transform center = new GameObject().transform;
        center.parent = transform;
        center.localPosition = commandModule.transform.localPosition;
        center.localRotation = commandModule.transform.localRotation;
        foreach (Transform child in children)
        {
            child.transform.parent = center.transform;
        }
        center.localPosition = Vector3.zero;
        center.localRotation = Quaternion.identity;
        foreach (Transform child in children)
        {
            child.transform.parent = transform;
        }
        Destroy(center.gameObject);
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Enable physics
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in allRigidbodies)
        {
            rb.isKinematic = false;
        }

        // Remove visuals
        int shieldLayer = LayerMask.NameToLayer("Shield");
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag("BuildVisualWidget"))
            {
                t.gameObject.SetActive(false);
            }
            if (t.gameObject.layer == shieldLayer)
            {
                Destroy(t.gameObject);
            }
        }
        // I am not sure why but the command module will have a sphere collider on it for some reason and it does mess things up
        SphereCollider sphereCollider = commandModule.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            Destroy(sphereCollider);
        }
        // All done converting! Script is not needed at runtime.
        Destroy(this);
    }
}
