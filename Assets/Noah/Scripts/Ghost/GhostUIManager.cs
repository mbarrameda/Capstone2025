using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GhostUIManager : MonoBehaviour
{
    [Header("Fear UI")]
    public Slider fearBar;
    public Image fearFill;
    public Color fullFearColor = Color.green;
    public Color mediumFearColor = Color.yellow;
    public Color lowFearColor = Color.red;
    public float lowFearThreshold = 25f;

    [Header("Stun Status - TMP")]
    public TextMeshProUGUI stunStatusText;
    public Color stunTextColor = Color.red;

    [Header("Ability Buttons")]
    public Image phaseButton;
    public Image transformButton;

    [Header("Button Colors")]
    public Color activeColor = Color.white;
    public Color cooldownColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
    public Color unavailableColor = new Color(0.4f, 0.4f, 0.4f, 0.2f);
    public Color notEnoughFearColor = new Color(1f, 0.5f, 0f, 0.6f);

    // Button cooldown tracking
    private bool phaseOnCooldown = false;
    private float phaseCooldownTimer = 0f;
    private bool transformOnCooldown = false;
    private float transformCooldownTimer = 0f;

    private GhostController ghost;
    private CanvasGroup fearCanvasGroup;
    private bool wasStunned = false;
    private bool isInitialized = false;

    void Start()
    {
        InitializeUI();
    }

    void InitializeUI()
    {
        if (SceneUIManager.Instance != null)
        {
            fearBar = SceneUIManager.Instance.fearBar;
            fearFill = SceneUIManager.Instance.fearFill;
            stunStatusText = SceneUIManager.Instance.stunStatusText;
            phaseButton = SceneUIManager.Instance.phaseButton;
            transformButton = SceneUIManager.Instance.transformButton;
        }

        ghost = GetComponentInParent<GhostController>();

        if (ghost == null)
        {
            ghost = GetComponent<GhostController>();
        }

        if (ghost == null)
        {
           // Debug.LogError("GhostUIManager: No GhostController found!");
            return;
        }

        InitializeFearUI();
        InitializeStunStatus();
        InitializeAbilityButtons();

        isInitialized = true;
        Debug.Log("GhostUIManager initialized successfully");
    }

    void InitializeFearUI()
    {
        if (fearBar != null)
        {
            fearBar.minValue = 0f;
            fearBar.maxValue = 100f;
            fearBar.value = ghost.fear;

            fearCanvasGroup = fearBar.GetComponent<CanvasGroup>();
            if (fearCanvasGroup == null)
            {
                fearCanvasGroup = fearBar.gameObject.AddComponent<CanvasGroup>();
            }

            fearCanvasGroup.alpha = 1f;
        }
        else
        {
            Debug.LogError("FearBar is not assigned!");
        }
    }

    void InitializeStunStatus()
    {
        if (stunStatusText != null)
        {
            stunStatusText.color = stunTextColor;
            stunStatusText.text = "";
            stunStatusText.gameObject.SetActive(false);
        }
    }

    void InitializeAbilityButtons()
    {
        UpdateAbilityButtonAppearance();
    }

    void Update()
    {
        if (!isInitialized || ghost == null) return;

        UpdateFearUI();
        UpdateStunStatus();
        UpdateAbilityButtons();
        UpdateCooldownTimers();
    }

    void UpdateFearUI()
    {
        if (fearBar == null) return;

        float currentFear = ghost.fear;
        fearBar.value = currentFear;

        if (fearFill != null)
        {
            if (currentFear <= lowFearThreshold) fearFill.color = lowFearColor;
            else if (currentFear <= 50f) fearFill.color = mediumFearColor;
            else fearFill.color = fullFearColor;
        }
    }

    void UpdateStunStatus()
    {
        if (stunStatusText == null) return;

        bool currentStunState = ghost.isStunned;

        if (currentStunState != wasStunned)
        {
            if (currentStunState)
            {
                stunStatusText.text = "STUNNED!";
                stunStatusText.gameObject.SetActive(true);
                StartCoroutine(PulseStunText());
            }
            else
            {
                stunStatusText.text = "";
                stunStatusText.gameObject.SetActive(false);
                StopAllCoroutines();
            }
            wasStunned = currentStunState;
        }
    }

    void UpdateAbilityButtons()
    {
        UpdateAbilityButtonAppearance();
    }

    void UpdateAbilityButtonAppearance()
    {
        // Phase Button logic
        if (phaseButton != null)
        {
            if (phaseOnCooldown) phaseButton.color = cooldownColor;
            else if (ghost.fear < ghost.phaseCost) phaseButton.color = notEnoughFearColor;
            else if (ghost.isStunned) phaseButton.color = unavailableColor;
            else phaseButton.color = activeColor;
        }

        // Transform Button logic
        if (transformButton != null)
        {
            if (transformOnCooldown || ghost.isStunned || ghost.isControllingClone)
                transformButton.color = unavailableColor;
            else
                transformButton.color = activeColor;
        }
    }

    void UpdateCooldownTimers()
    {
        if (phaseOnCooldown)
        {
            phaseCooldownTimer -= Time.deltaTime;
            if (phaseCooldownTimer <= 0f) phaseOnCooldown = false;
        }

        if (transformOnCooldown)
        {
            transformCooldownTimer -= Time.deltaTime;
            if (transformCooldownTimer <= 0f) transformOnCooldown = false;
        }
    }

    public void TriggerPhaseCooldown(float duration = 1f)
    {
        phaseOnCooldown = true;
        phaseCooldownTimer = duration;
    }

    public void TriggerTransformCooldown(float duration = 0.5f)
    {
        transformOnCooldown = true;
        transformCooldownTimer = duration;
    }

    IEnumerator PulseStunText()
    {
        while (ghost != null && ghost.isStunned && stunStatusText != null)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f; // Faster pulse
            Color pulseColor = stunTextColor;
            pulseColor.a = pulse;
            stunStatusText.color = pulseColor;
            yield return null;
        }
    }

    public void SetFearUIVisible(bool visible)
    {
        if (fearCanvasGroup != null)
        {
            fearCanvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}