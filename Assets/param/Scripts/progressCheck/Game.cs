using UnityEngine;
using UnityEngine.UI;

public class GameTracker : MonoBehaviour
{
    [Header("Progress Fill")]
    [Tooltip("Assign the UI Image with Fill enabled.")]
    public Image progressFill;

    [Header("Game Settings")]
    [Tooltip("Maximum number of changes possible.")]
    public int maxTotalChanges = 10;

    [Header("Current State")]
    [SerializeField]
    private int currentChangeCount = 0;

    void Start()
    {
        InitializeFill();

        ChangeTracker.OnChangeOccurred += HandleChange;

        if (progressFill == null)
        {
            Debug.LogError("Progress Fill Image is not assigned!");
        }
    }

    private void InitializeFill()
    {
        if (progressFill != null)
        {
            progressFill.fillAmount = 0f; // Start empty
        }
    }

    private void HandleChange()
    {
        currentChangeCount++;

        float normalizedProgress =
            (float)Mathf.Clamp(currentChangeCount, 0, maxTotalChanges) / maxTotalChanges;

        if (progressFill != null)
        {
            progressFill.fillAmount = normalizedProgress;
        }

        if (currentChangeCount >= maxTotalChanges)
        {
            Debug.Log("Maximum changes reached! Game state stabilized.");
        }
    }

    void OnDestroy()
    {
        ChangeTracker.OnChangeOccurred -= HandleChange;
    }
}