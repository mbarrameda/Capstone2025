using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

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
    public Slider batterySlider;
    public Image batteryFill;
    public Color fullBatteryColor = Color.green;
    public Color mediumBatteryColor = Color.yellow;
    public Color lowBatteryColor = Color.red;
    public float lowBatteryThreshold = 20f;

    private Slider batteryBar;
    private bool batteryBarFound = false;
    public float currentBattery; 
    private bool flashlightOn = false;
    private bool isOut = false;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private CanvasGroup batteryCanvasGroup;

    private void Start()
    {
        if (BatteryManager.Instance == null)
        {
            new GameObject("BatteryManager").AddComponent<BatteryManager>();
        }
        // Find battery bar
        GameObject barObject = GameObject.Find("BatteryBar");
        if (barObject != null)
        {
            batteryBar = barObject.GetComponent<Slider>();
            if (batteryBar != null)
            {
                batteryBarFound = true;
                batteryBar.gameObject.SetActive(false);
            }
        }
    }
    private void Awake()
    {
        if (flashlight != null) flashlight.enabled = false;
        currentBattery = batteryMax;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        InitializeBatteryUI();
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
        UpdateBatteryUI();
    }

    private void InitializeBatteryUI()
    {
        if (batterySlider != null)
        {
            batterySlider.minValue = 0f;
            batterySlider.maxValue = batteryMax;
            batterySlider.value = currentBattery;

            // Get or add canvas group for fading
            batteryCanvasGroup = batterySlider.GetComponent<CanvasGroup>();
            if (batteryCanvasGroup == null)
            {
                batteryCanvasGroup = batterySlider.gameObject.AddComponent<CanvasGroup>();
            }

            // Start hidden
            batteryCanvasGroup.alpha = 0f;
            batterySlider.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Battery Slider not assigned in Inspector - battery UI will not work");
        }
    }

    private void UpdateBatteryUI()
    {
        if (batterySlider == null) return;

        batterySlider.value = currentBattery;

        if (batteryFill != null)
        {
            float batteryPercent = (currentBattery / batteryMax) * 100f;

            if (batteryPercent <= lowBatteryThreshold)
            {
                batteryFill.color = lowBatteryColor;
            }
            else if (batteryPercent <= 50f)
            {
                batteryFill.color = mediumBatteryColor;
            }
            else
            {
                batteryFill.color = fullBatteryColor;
            }
        }
    }

    public void TogglePhone()
    {
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
        bool stunnedAnyGhost = false;

        foreach (GhostController ghost in ghosts)
        {
            Vector3 dirToGhost = ghost.transform.position - origin;
            float distance = dirToGhost.magnitude;

            if (distance > stunDistance) continue;

            float angle = Vector3.Angle(forward, dirToGhost);
            if (angle <= stunAngle / 2f)
            {
                if (Physics.Raycast(origin, dirToGhost.normalized, out RaycastHit hit, stunDistance))
                {
                    if (hit.collider.gameObject == ghost.gameObject)
                    {
                        ghost.Stun(stunDuration);
                        stunnedAnyGhost = true;
                    }
                }
            }
        }

        // Show stun text if we stunned any ghost
        if (stunnedAnyGhost && StunTextDisplay.Instance != null)
        {
            StunTextDisplay.Instance.ShowStunText(0f);
        }
    }

    private IEnumerator FadeBatteryUI(float targetAlpha, float duration)
    {
        if (batteryCanvasGroup == null) yield break;

        float startAlpha = batteryCanvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            batteryCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        batteryCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f && batterySlider != null)
        {
            batterySlider.gameObject.SetActive(false);
        }
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