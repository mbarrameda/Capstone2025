using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayMenuController : MonoBehaviour
{
    [Header("Pause Panel")]
    public GameObject pausePanel;

    private bool pauseOpen = false;

    private Gamepad controllingGamepad; // The first controller that connects

    void Start()
    {
        pausePanel.SetActive(false);

        // Assign the first connected gamepad at start
        if (Gamepad.all.Count > 0)
        {
            controllingGamepad = Gamepad.all[0];
            Debug.Log("Controller assigned: " + controllingGamepad.name);
        }
    }

    void Update()
    {
        // No controller connected
        if (controllingGamepad == null) return;

        // B button toggles pause panel
        if (controllingGamepad.buttonEast.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        pauseOpen = !pauseOpen;
        pausePanel.SetActive(pauseOpen);
    }
}