using UnityEngine;
using UnityEngine.UI; // Required for using the Slider component

public class GameTracker : MonoBehaviour
{
    [Header("Slider Settings")]
    [Tooltip("Assign the Slider that belongs to the Ghost (decreases on change).")]
    public Slider ghostSlider;

    [Tooltip("Assign the Slider that belongs to the Explorer (increases on change).")]
    public Slider explorerSlider;

    [Tooltip("The total number of tracked objects/changes possible in the game.")]
    public int maxTotalChanges = 10;

    [Header("Current State")]
    [Tooltip("Internal count of changes that have occurred.")]
    [SerializeField] // Display in Inspector, but not editable
    private int currentChangeCount = 0;

    void Start()
    {
        // 1. Initial setup of Sliders
        InitializeSliders();

        // 2. Subscribe to the static event
        ChangeTracker.OnChangeOccurred += HandleChange;

        // 3. Optional: Initial check to ensure all components are linked
        if (ghostSlider == null || explorerSlider == null)
        {
            Debug.LogError("Sliders are not assigned in the Inspector! Please link them.");
        }
    }

    // Called once on Start to set the initial values and range
    private void InitializeSliders()
    {
        // Normalize max changes to 1.0 for easier calculation
        if (ghostSlider != null)
        {
            ghostSlider.maxValue = 1f;
            // Ghost starts full (100% chance/power/whatever is being lost)
            ghostSlider.value = 1f;
        }

        if (explorerSlider != null)
        {
            explorerSlider.maxValue = 1f;
            // Explorer starts empty (0% progress)
            explorerSlider.value = 0f;
        }
    }

    // This method is called every time the ChangeTracker event is triggered
    private void HandleChange()
    {
        // Increment the count of changes that have occurred
        currentChangeCount++;

        // Calculate the current normalized progress (0 to 1)
        // We clamp the count to ensure it doesn't exceed the max (maxTotalChanges)
        float normalizedProgress = (float)Mathf.Clamp(currentChangeCount, 0, maxTotalChanges) / maxTotalChanges;

        // Update the Ghost Slider (Decreases as changes occur)
        if (ghostSlider != null)
        {
            // Ghost slider goes DOWN from 1.0 to 0.0
            ghostSlider.value = 1f - normalizedProgress;
        }

        // Update the Explorer Slider (Increases as changes occur)
        if (explorerSlider != null)
        {
            // Explorer slider goes UP from 0.0 to 1.0
            explorerSlider.value = normalizedProgress;
        }

        // Example: Check for game end condition
        if (currentChangeCount >= maxTotalChanges)
        {
            Debug.Log("Maximum changes reached! Game state stabilized.");
        }
    }

    // IMPORTANT: Always unsubscribe from events when the object is destroyed
    void OnDestroy()
    {
        ChangeTracker.OnChangeOccurred -= HandleChange;
    }
}