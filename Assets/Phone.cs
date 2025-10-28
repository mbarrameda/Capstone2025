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

        // Turn off flashlight if phone is put away
        if (!isOut)
        {
            flashlightOn = false;
            if (flashlight != null)
                flashlight.enabled = false;
        }
    }

    public void ToggleFlashlight()
    {
        if (!isOut) return; // only works if phone is out
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
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        foreach (GhostController ghost in ghosts)
        {
            Vector3 dirToGhost = ghost.transform.position - flashlight.transform.position;
            float distance = dirToGhost.magnitude;
            if (distance > stunDistance) continue;

            float angle = Vector3.Angle(flashlight.transform.forward, dirToGhost);
            if (angle <= stunAngle / 2f)
            {
                ghost.Stun(stunDuration);
            }
        }
    }
}
