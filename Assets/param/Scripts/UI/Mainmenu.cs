using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // NEW Input System
using TMPro;

public class MultiControllerReady : MonoBehaviour
{
    [Header("UI for each controller")]
    public TextMeshProUGUI[] readyTexts;   // One text per controller

    [Header("Scene to load when all ready")]
    public string nextSceneName = "NextScene";

    private bool[] isReady;

    private void Start()
    {
        int controllerCount = Gamepad.all.Count;

        // Prepare ready-tracking array
        isReady = new bool[controllerCount];

        // Hide all texts at start
        foreach (var t in readyTexts)
            t.gameObject.SetActive(false);

        Debug.Log("Controllers Connected: " + controllerCount);
    }

    private void Update()
    {
        // Keyboard fallback
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            ManuallySetReady(0); // Assign keyboard as controller 0
        }

        // Check all gamepads
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            Gamepad pad = Gamepad.all[i];

            // A button (Xbox) or Cross button (PS)
            if (pad != null && pad.buttonSouth.wasPressedThisFrame)
            {
                SetControllerReady(i);
            }
        }

        // Load scene when all ready
        if (CheckAllReady())
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void SetControllerReady(int index)
    {
        if (isReady[index]) return; // Already ready

        isReady[index] = true;
        readyTexts[index].gameObject.SetActive(true);
        readyTexts[index].text = $"Controller {index + 1}: READY!";
        Debug.Log($"Controller {index + 1} ready");
    }

    // Keyboard override
    private void ManuallySetReady(int index)
    {
        if (index < readyTexts.Length)
        {
            SetControllerReady(index);
        }
    }

    private bool CheckAllReady()
    {
        foreach (var r in isReady)
        {
            if (!r) return false;
        }
        return true;
    }
}
