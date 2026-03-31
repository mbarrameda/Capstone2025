using UnityEngine;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro support
using System;

public class GameTracker : MonoBehaviour
{
    [Serializable]
    public struct TextThreshold
    {
        public TextMeshProUGUI textElement;
        public int changesRequired;
    }

    [Header("Progress Fill")]
    [Tooltip("Assign the UI Image with Fill enabled.")]
    public Image progressFill;

    [Header("Text Elements")]
    public TextThreshold[] textThresholds;

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
            Debug.LogError("Progress Fill Image is not assigned!");
    }

    private void InitializeFill()
    {
        if (progressFill != null)
            progressFill.fillAmount = 0f;
    }

    private void HandleChange()
    {
        currentChangeCount++;

        // Update Fill Amount
        float normalizedProgress = (float)Mathf.Clamp(currentChangeCount, 0, maxTotalChanges) / maxTotalChanges;
        if (progressFill != null)
            progressFill.fillAmount = normalizedProgress;

        // Check and remove text elements based on thresholds
        UpdateTextElements();

        if (currentChangeCount >= maxTotalChanges)
            Debug.Log("Maximum changes reached! Game state stabilized.");
    }

    private void UpdateTextElements()
    {
        foreach (var item in textThresholds)
        {
            // If the element exists and we've reached/passed the threshold, deactivate it
            if (item.textElement != null && currentChangeCount >= item.changesRequired)
            {
                item.textElement.gameObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        ChangeTracker.OnChangeOccurred -= HandleChange;
    }
}