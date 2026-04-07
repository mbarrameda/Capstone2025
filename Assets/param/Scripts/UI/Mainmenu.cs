using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // NEW Input System
using TMPro;

public class MultiControllerReady : MonoBehaviour
{
    [Header("UI for each controller")]
    [Tooltip("The number of elements here determines the max player count.")]
    public TextMeshProUGUI[] readyTexts;

    [Header("Scene to load when all ready")]
    public string nextSceneName = "NextScene";

    private bool[] isReady;

    private void Start()
    {
        // FIX: Initialize the array based on UI slots, not just physical gamepads.
        // This ensures Player 2's slot exists even if the controller isn't detected yet.
        isReady = new bool[readyTexts.Length];

        // Hide all texts at start and verify assignments
        for (int i = 0; i < readyTexts.Length; i++)
        {
            if (readyTexts[i] != null)
            {
                readyTexts[i].gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Ready Text at element {i} is not assigned in the Inspector!");
            }
        }

        Debug.Log($"System initialized for up to {readyTexts.Length} players.");
    }

    private void Update()
    {
        // Keyboard fallback (Assigns to Player 1)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SetControllerReady(0);
        }

        // Check all connected gamepads
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            // Safety: Don't try to set player 3 as ready if you only have 2 UI slots
            if (i >= isReady.Length) break;

            Gamepad pad = Gamepad.all[i];

            // Check if South button (A/Cross) was pressed
            if (pad != null && pad.buttonSouth.wasPressedThisFrame)
            {
                SetControllerReady(i);
            }
        }

        // Load scene when all players are ready
        if (CheckAllReady())
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void SetControllerReady(int index)
    {
        // Basic boundary and "already ready" checks
        if (index < 0 || index >= isReady.Length || isReady[index]) return;

        isReady[index] = true;

        if (readyTexts[index] != null)
        {
            readyTexts[index].gameObject.SetActive(true);
            readyTexts[index].text = $"Player {index + 1}: READY!";
        }

        Debug.Log($"Player {index + 1} is now ready.");
    }

    private bool CheckAllReady()
    {
        // If no players are ready yet, don't proceed
        bool anyReady = false;

        // Ensure every slot that IS active/connected is ready
        // Note: This logic requires ALL UI slots to be filled. 
        // If you only want CONNECTED players to be ready, see the note below.
        for (int i = 0; i < isReady.Length; i++)
        {
            if (!isReady[i]) return false;
            anyReady = true;
        }

        return anyReady;
    }
}