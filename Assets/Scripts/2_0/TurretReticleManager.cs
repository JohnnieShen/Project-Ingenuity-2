using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TurretReticleManager : MonoBehaviour
{
    [Header("Setup")]
    public Camera mainCamera;
    public Transform vehicleRoot;
    public GameObject dotPrefab;
    public RectTransform reticleContainer;

    [Header("Settings")]
    public float ringRadius = 50f;
    public float reticleSmoothSpeed = 15f;

    [Header("Colors")]
    public Color colorAimed = Color.green;
    public Color colorTurning = Color.yellow;
    public Color colorBlocked = Color.red;

    private List<Turret> turrets = new List<Turret>();
    private List<Image> dotImages = new List<Image>();

    void Start()
    {
        RefreshTurretList();

        if (ModeSwitcher.instance != null)
        {
            ModeSwitcher.instance.OnModeChanged += HandleModeChange;
        }
    }

    void OnDestroy()
    {
        if (ModeSwitcher.instance != null)
        {
            ModeSwitcher.instance.OnModeChanged -= HandleModeChange;
        }
    }

    private void HandleModeChange(ModeSwitcher.Mode oldMode, ModeSwitcher.Mode newMode)
    {
        if (newMode == ModeSwitcher.Mode.Drive)
        {
            RefreshTurretList();
        }

        if (reticleContainer != null)
        {
            reticleContainer.gameObject.SetActive(newMode == ModeSwitcher.Mode.Drive);
        }
    }

    void RefreshTurretList()
    {
        turrets.Clear();
        foreach (var img in dotImages)
        {
            if (img != null) Destroy(img.gameObject);
        }
        dotImages.Clear();

        if (vehicleRoot != null)
        {
            turrets.AddRange(vehicleRoot.GetComponentsInChildren<Turret>());
        }

        if (turrets.Count == 0) return;

        float angleStep = 360f / turrets.Count;

        for (int i = 0; i < turrets.Count; i++)
        {
            GameObject newDot = Instantiate(dotPrefab, reticleContainer);

            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * ringRadius;
            float y = Mathf.Sin(angle) * ringRadius;

            newDot.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            dotImages.Add(newDot.GetComponent<Image>());
        }
    }

    void Update()
    {
        if (ModeSwitcher.instance != null && ModeSwitcher.instance.currentMode != ModeSwitcher.Mode.Drive)
            return;

        if (turrets.Count == 0 || mainCamera == null) return;

        UpdateReticlePosition();
        UpdateDotColors();
    }

    void UpdateReticlePosition()
    {
        if (FreeCameraLook.instance == null || FreeCameraLook.instance.aimTarget == null)
            return;

        Transform target = FreeCameraLook.instance.aimTarget;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

        if (screenPos.z < 0)
        {
            reticleContainer.gameObject.SetActive(false);
        }
        else
        {
            if (!reticleContainer.gameObject.activeSelf)
                reticleContainer.gameObject.SetActive(true);

            screenPos.z = 0f;
            reticleContainer.position = Vector3.Lerp(reticleContainer.position, screenPos, Time.deltaTime * reticleSmoothSpeed);
        }
    }

    void UpdateDotColors()
    {
        for (int i = 0; i < turrets.Count; i++)
        {
            Turret t = turrets[i];

            if (t == null || i >= dotImages.Count) continue;

            Image dot = dotImages[i];

            if (t.isBlocked)
            {
                dot.color = colorBlocked;
                continue;
            }

            if (t.shootPoint != null && FreeCameraLook.instance != null && FreeCameraLook.instance.aimTarget != null)
            {
                Vector3 directionToTarget = (FreeCameraLook.instance.aimTarget.position - t.shootPoint.position).normalized;
                float angleError = Vector3.Angle(t.shootPoint.forward, directionToTarget);

                if (angleError < t.aimTolerance)
                {
                    dot.color = colorAimed;
                }
                else
                {
                    dot.color = colorTurning;
                }
            }
            else
            {
                dot.color = colorTurning;
            }
        }
    }
}