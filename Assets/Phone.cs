using UnityEngine;
using UnityEngine.InputSystem;

public class Phone : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Light flashlight;
    public float batteryMax = 100f;
    public float batteryDrainPerSecond = 10f;
    public float stunDuration = 7f;
    public float stunAngle = 30f;
    public float stunDistance = 10f;

    [Header("References")]
    public PlayerInputHandler explorer;

    [Header("Phone Settings")]
    public float pullOutSpeed = 5f;

    private float currentBattery;
    private bool flashlightOn = false;
    private bool isOut = false;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        if (flashlight != null) flashlight.enabled = false;
        currentBattery = batteryMax;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (flashlightOn)
        {
            DrainBattery();
            StunGhostsInCone();
        }
    }

    public void TogglePhone()
    {
        isOut = !isOut;
        gameObject.SetActive(isOut);

        if (isOut)
        {
            // Automatically turn flashlight on
            if (currentBattery > 0f)
            {
                flashlightOn = true;
                if (flashlight != null)
                    flashlight.enabled = true;
            }
        }
        else
        {
            // Turn flashlight off when putting phone away
            flashlightOn = false;
            if (flashlight != null)
                flashlight.enabled = false;
        }
    }

    // This stays in case you need manual toggle later
    public void ToggleFlashlight()
    {
        if (!isOut) return;
        if (currentBattery <= 0f)
        {
            flashlightOn = false;
            flashlight.enabled = false;
            return;
        }

        flashlightOn = !flashlightOn;
        if (flashlight != null) flashlight.enabled = flashlightOn;
    }

    private void DrainBattery()
    {
        currentBattery -= batteryDrainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Max(currentBattery, 0f);
        if (currentBattery <= 0f)
        {
            flashlightOn = false;
            if (flashlight != null) flashlight.enabled = false;
        }
    }

    private void StunGhostsInCone()
    {
        if (explorer == null || explorer.cameraTransform == null) return;

        Vector3 origin = explorer.cameraTransform.position;
        Vector3 forward = explorer.cameraTransform.forward;

        GhostController[] ghosts = FindObjectsOfType<GhostController>();

        foreach (GhostController ghost in ghosts)
        {
            Vector3 dirToGhost = ghost.transform.position - origin;
            float distance = dirToGhost.magnitude;

            if (distance > stunDistance) continue;

            float angle = Vector3.Angle(forward, dirToGhost);
            if (angle <= stunAngle / 2f)
            {
                // Only stun if ghost is visible (no walls between)
                if (Physics.Raycast(origin, dirToGhost.normalized, out RaycastHit hit, stunDistance))
                {
                    if (hit.collider.gameObject == ghost.gameObject)
                    {
                        ghost.Stun(stunDuration);
                    }
                }
            }
        }
    }
}
