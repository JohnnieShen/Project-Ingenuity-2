using System.Collections.Generic;
using UnityEngine;

public class Saw : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private LayerMask damageLayers;

    [Header("VFX")]
    [SerializeField] private GameObject hitParticlesPrefab;
    [SerializeField] private float particleCooldown = 0.1f;

    private readonly Dictionary<BlockHealth, float> particleTimers = new();

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & damageLayers) == 0)
            return;

        BlockHealth health = other.GetComponentInParent<BlockHealth>();
        if (health == null)
            return;

        float damage = damagePerSecond * Time.deltaTime;
        health.TakeDamage(damage);

        float timer;
        particleTimers.TryGetValue(health, out timer);
        timer -= Time.deltaTime;

        if (hitParticlesPrefab != null && timer <= 0f)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Instantiate(hitParticlesPrefab, hitPoint, Quaternion.identity);

            timer = particleCooldown;
        }

        particleTimers[health] = timer;
    }

    private void OnTriggerExit(Collider other)
    {
        BlockHealth health = other.GetComponentInParent<BlockHealth>();
        if (health != null && particleTimers.ContainsKey(health))
        {
            particleTimers.Remove(health);
        }
    }
}
