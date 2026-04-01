using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Battery UI - MANUALLY ASSIGN IN INSPECTOR")]
    public float currentBattery; 
    private bool flashlightOn = false;
    private bool isOut = false;

    [Header("Stun Charge Settings")]
    public float timeToStun = 3f; // how long the light must be held on the ghost
    private Dictionary<GhostController, float> stunChargeTimers = new Dictionary<GhostController, float>();

    private void Awake()
    {
        if (flashlight != null) flashlight.enabled = false;
        currentBattery = batteryMax;
        

    }

    private void Update()
    {
        if (flashlightOn)
        {
            DrainBattery();
            StunGhostsInCone();

            // Update battery value in BatteryManager
            if (BatteryManager.Instance != null)
            {
                BatteryManager.Instance.SetBatteryValue(currentBattery);
            }
        }
    }

    public void TogglePhone()
    {

        if (explorer != null && !explorer.shouldFindUI)
        {
            // Clone phone - do nothing or handle differently
            return;
        }

        isOut = !isOut;
        gameObject.SetActive(isOut);

        // Tell BatteryManager about phone state
        if (BatteryManager.Instance != null)
        {
            BatteryManager.Instance.SetPhoneState(isOut);
        }

        if (isOut)
        {
            // Automatically turn flashlight on if we have battery
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

            // Reset all charge timers — putting the phone away breaks the stun charge
            stunChargeTimers.Clear();
        }
    }

    public void ToggleFlashlight()
    {
        if (!isOut) return;
        if (currentBattery <= 0f)
        {
            flashlightOn = false;
            if (flashlight != null)
                flashlight.enabled = false;
            return;
        }

        flashlightOn = !flashlightOn;
        if (flashlight != null)
            flashlight.enabled = flashlightOn;
    }

    private void DrainBattery()
    {
        currentBattery -= batteryDrainPerSecond * Time.deltaTime;
        currentBattery = Mathf.Max(currentBattery, 0f);

        // Update battery value in BatteryManager
        if (BatteryManager.Instance != null)
        {
            BatteryManager.Instance.SetBatteryValue(currentBattery);
        }

        if (currentBattery <= 0f)
        {
            flashlightOn = false;
            if (flashlight != null)
                flashlight.enabled = false;
        }
    }

    private void StunGhostsInCone()
    {
        if (explorer == null || explorer.cameraTransform == null) return;

        Vector3 origin = explorer.cameraTransform.position;
        Vector3 forward = explorer.cameraTransform.forward;

        GhostController[] ghosts = FindObjectsOfType<GhostController>();

        // Track which ghosts are currently in the cone this frame
        bool stunnedAnyGhost = false;
        System.Collections.Generic.HashSet<GhostController> ghostsInCone
            = new System.Collections.Generic.HashSet<GhostController>();

        foreach (GhostController ghost in ghosts)
        {
            if (ghost.isStunned) continue; // already stunned, skip

            Vector3 dirToGhost = ghost.transform.position - origin;
            float distance = dirToGhost.magnitude;

            if (distance > stunDistance) continue;

            float angle = Vector3.Angle(forward, dirToGhost);
            if (angle > stunAngle / 2f) continue;

            if (!Physics.Raycast(origin, dirToGhost.normalized, out RaycastHit hit, stunDistance)) continue;
            if (hit.collider.gameObject != ghost.gameObject) continue;

            // Ghost is in the cone and has line of sight — charge the timer
            ghostsInCone.Add(ghost);

            if (!stunChargeTimers.ContainsKey(ghost))
                stunChargeTimers[ghost] = 0f;

            stunChargeTimers[ghost] += Time.deltaTime;

            if (stunChargeTimers[ghost] >= timeToStun)
            {
                ghost.Stun(stunDuration);
                stunChargeTimers.Remove(ghost);
                stunnedAnyGhost = true;
            }
        }

        // Reset timers for any ghost that left the cone this frame
        var ghostsToReset = new System.Collections.Generic.List<GhostController>();
        foreach (var kvp in stunChargeTimers)
        {
            if (!ghostsInCone.Contains(kvp.Key))
                ghostsToReset.Add(kvp.Key);
        }
        foreach (var ghost in ghostsToReset)
            stunChargeTimers.Remove(ghost);

        if (stunnedAnyGhost && StunTextDisplay.Instance != null)
            StunTextDisplay.Instance.ShowStunText(0f);
    }

    public bool IsPhoneOut()
    {
        return isOut;
    }

    public float GetBatteryLevel()
    {
        return currentBattery;
    }

}