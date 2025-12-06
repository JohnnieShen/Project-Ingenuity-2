using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
    * This script is responsible for the movement of the enemy.
    * It should be used in conjunction with the EnemyAI script.
    * For now, to manipulate movement, you just need to set the targetPosition variable to the target position you want the enemy to move towards.
    */  
public class EnemyMovement : MonoBehaviour
{
    /*
    * Author: Johnny
    * Summary: This script is responsible for the movement of the enemy vehicle in the game. It uses a Rigidbody component to apply forces to the vehicle and control its movement.
    * The script calculates the target speed based on the distance to the target position and applies steering and braking forces to the vehicle.
    * The script also handles the movement of the wheels and applies forces to them based on the current drive and steer input.
    * The script is designed to be used in conjunction with the EnemyAI script to control the enemy vehicle's behavior.
    */
    public float maxSpeed = 20f;
    public float arrivalDistance = 2f;
    public float reverseThreshold = 0.5f;
    public float steeringSharpness = 2f;
    public float accelerationSharpness = 2f;
    public float brakeStrength = 100f;
    public float slowDownRadius = 10f;

    public Transform targetPosition; // Change this for movement
    public Rigidbody rb;
    public Wheel[] wheels;

    public float currentDriveInput;
    public float currentSteerInput;
    
    // TODO bool for if movement is enabled

    private EnemyAI enemyAI;

    // void Awake()
    // {
    //     enemyAI = GetComponent<EnemyAI>();
    //     if (enemyAI == null) {
    //         Debug.LogError("EnemyAI component not found on this GameObject. Please add it to the same GameObject as EnemyMovement.");
    //         return;
    //     }

    //     if (!enemyAI.enabled)
    //     {
    //         enemyAI.enabled = true;
    //         if (!enemyAI.enabled)
    //         {
    //             StartCoroutine(ReEnableAfterDelay(5f));
    //         }
    //     }
    // }

    private IEnumerator ReEnableAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (enemyAI != null && !enemyAI.enabled)
        {
            Debug.Log("Re-enabling enemy AI after delay.");
            enemyAI.enabled = true;
        }
    }

    /* FixedUpdate is called every fixed framerate frame, if the MonoBehaviour is enabled.
    * In this case, it is used to calculate the movement of the enemy vehicle towards the target position.
    * It calculates the vector to the target position, the distance to the target position, and the local target direction.
    */
    void FixedUpdate()
    {
        if (targetPosition == null) return;

        Vector3 toTarget = targetPosition.position - transform.position; // Calculate the vector to the target position.
        float distanceToTarget = toTarget.magnitude; // Calculate the distance to the target position.
        
        Vector3 localTargetDir = transform.InverseTransformDirection(toTarget.normalized); // Calculate the local target direction.
        currentSteerInput = Mathf.Clamp(localTargetDir.x * steeringSharpness, -1f, 1f); // Calculate the steering input based on the local target direction.

        float targetSpeed = CalculateTargetSpeed(toTarget, distanceToTarget); // Calculate the target speed based on the distance to the target.
        UpdateDriveInput(targetSpeed, distanceToTarget);
        UpdateWheels();
        ApplyBraking(distanceToTarget);
    }

    // Calculate the target speed based on the distance to the target.
    // If we are within the slow down radius, lerp the speed to 0 based on the distance.
    // If we are within the arrival distance, return 0 speed.
    // If we are not within the slow down radius, return the maximum speed.
    // Param 1: toTarget - The vector to the target position.
    // Param 2: distance - The distance to the target position.
    // Return: The target speed based on the distance to the target position.
    float CalculateTargetSpeed(Vector3 toTarget, float distance)
    {
        float forwardDot = Vector3.Dot(transform.forward, toTarget.normalized); // Calculate the dot product between the forward vector and the vector to the target to see if we are going forward or not.
        bool shouldReverse = forwardDot < -reverseThreshold; // Check if we should reverse based on the dot product.

        if (shouldReverse) return -maxSpeed * 0.5f; // If we should reverse, return a negative speed.
        if (distance < arrivalDistance) return 0f;

        if (distance < slowDownRadius)
        {
            return Mathf.Lerp(0, maxSpeed, distance / slowDownRadius); // If we are within the slow down radius, lerp the speed to 0 based on the distance.
        }
        
        return maxSpeed; // Otherwise, return the maximum speed.
    }

    // Update the drive input based on the target speed and the distance to the target.
    // Param 1: targetSpeed - The target speed based on the distance to the target position.
    // Param 2: distanceToTarget - The distance to the target position.
    void UpdateDriveInput(float targetSpeed, float distanceToTarget)
    {
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward); // Calculate the current forward speed.
        float speedError = targetSpeed - currentForwardSpeed; // Calculate how far off the current speed is from the target speed.
        currentDriveInput = Mathf.Clamp(speedError * accelerationSharpness, -1f, 1f); // Clamp the drive input based on the speed error and the acceleration sharpness.

        if (distanceToTarget < arrivalDistance)
        {
            currentDriveInput = -Mathf.Clamp(currentForwardSpeed * 5f, -1f, 1f); // If we are within the arrival distance, reverse the drive input based on the current forward speed.
        }
    }

    // Apply braking based on the distance to the target.
    // If we are within the slow down radius, apply a braking force in the opposite direction of the velocity.
    // Param 1: distanceToTarget - The distance to the target position.
    void ApplyBraking(float distanceToTarget)
    {
        // Debug.Log("Distance to target: " + distanceToTarget);
        if (distanceToTarget < slowDownRadius) // If we are within the slow down radius, start breaking.
        {
            // Debug.Log("Applying braking");
            Vector3 brakeDirection = -rb.linearVelocity.normalized;
            float brakePower = Mathf.Lerp(1f, 0f, distanceToTarget / slowDownRadius);
            
            rb.AddForce(brakeDirection * brakeStrength * brakePower, ForceMode.Acceleration); 
            // TODO: only do this when there are actual wheels
        }
    }

    // Update the wheels based on the current drive and steer input.
    // It sets the drive input of the wheel to the current drive input and applies a force at the position of the wheel to help movement.
    // It also sets the steer angle of the wheel based on the current steer input and the maximum steering angle of the wheel.
    void UpdateWheels()
    {
        foreach (Wheel wheel in wheels)
        {
            if (wheel == null) continue; // Skip the wheel if it is null (aka destroyed in this context).
            Hull hull = wheel.GetComponentInParent<Hull>();
            if (hull != null && hull.canPickup) // Skip the wheel if it is attached to a block that can be picked up, aka detached.
                continue;
            wheel.driveInput = currentDriveInput; // Set the drive input of the wheel to the current drive input.
            float effectiveForce = wheel.accelForce * currentDriveInput; //
            if (rb != null && wheel != null) {
                rb.AddForceAtPosition( // Add a force at the position of the wheel to help movement, dirty fix for now.
                    wheel.transform.forward * effectiveForce,
                    wheel.transform.position,
                    ForceMode.Acceleration
                );
            }

            float sign = wheel.invertSteering ? -1f : 1f;
                
            float steerAngle = currentSteerInput * wheel.maxSteeringAngle * sign;
                
            wheel.currentSteerAngle = Mathf.Lerp(
                wheel.currentSteerAngle,
                steerAngle,
                Time.fixedDeltaTime * wheel.steeringReturnSpeed
            );
        }
    }

    /* OnDrawGizmos is called when the script is being edited in the Unity Editor.
    * It draws a line from the enemy vehicle to the target position and draws a wire sphere at the target position to visualize the arrival distance and slow down radius.
    * It also draws a wire sphere at the target position to visualize the slow down radius.
    * It is used for debugging purposes to visualize the target position and the distances.
    */
    void OnDrawGizmos()
    {
        if (targetPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPosition.position);
            Gizmos.DrawWireSphere(targetPosition.position, arrivalDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition.position, slowDownRadius);
        }
    }
}