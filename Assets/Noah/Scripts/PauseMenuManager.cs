using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Pause Menu Prefab")]
    public GameObject pauseMenuPrefab; // Prefab of the pause menu UI

    [Header("Controller Settings")]
    public float menuNavigationCooldown = 0.2f;

    private bool isPaused = false;
    private float lastNavigationTime = 0f;
    private List<PlayerInputHandler> explorers = new List<PlayerInputHandler>();
    private List<GhostController> ghosts = new List<GhostController>();

    // Track which player paused and their menu instance
    private MonoBehaviour pausingPlayer;
    private GameObject activePauseMenu;
    private Canvas activePauseCanvas;
    private Button resumeButton;
    private Button mainMenuButton;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Handle controller navigation in menu
        if (isPaused)
        {
            HandleMenuNavigation();
        }
    }

    public void PauseGame(MonoBehaviour triggeringPlayer)
    {
        if (isPaused) return;

        isPaused = true;
        pausingPlayer = triggeringPlayer;

        // Pause time
        Time.timeScale = 0f;

        // Create pause menu on the pausing player's display
        CreatePauseMenuForPlayer(triggeringPlayer);

        // Freeze all players and ghosts
        FreezeAllPlayers();

        Debug.Log($"Game Paused by {triggeringPlayer.GetType().Name}");
    }

    private void CreatePauseMenuForPlayer(MonoBehaviour player)
    {
        if (pauseMenuPrefab == null)
        {
            Debug.LogError("Pause menu prefab is not assigned!");
            return;
        }

        // Create the pause menu
        activePauseMenu = Instantiate(pauseMenuPrefab);
        activePauseCanvas = activePauseMenu.GetComponent<Canvas>();

        if (activePauseCanvas == null)
        {
            Debug.LogError("Pause menu prefab doesn't have a Canvas component!");
            return;
        }

        // Set the canvas to the correct display
        SetCanvasToPlayerDisplay(player, activePauseCanvas);

        // Get button references and set up listeners
        Button[] buttons = activePauseMenu.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name.Contains("Resume") || button.gameObject.name.Contains("Resume"))
            {
                resumeButton = button;
                resumeButton.onClick.AddListener(ResumeGame);
            }
            else if (button.name.Contains("MainMenu") || button.gameObject.name.Contains("MainMenu"))
            {
                mainMenuButton = button;
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        // Select the resume button for controller navigation
        if (resumeButton != null)
            resumeButton.Select();
    }

    private void SetCanvasToPlayerDisplay(MonoBehaviour player, Canvas canvas)
    {
        Camera playerCamera = null;
        int targetDisplay = 0;

        // Determine the player's camera and display
        if (player is PlayerInputHandler explorer)
        {
            playerCamera = explorer.playerCamera;
            if (playerCamera != null)
            {
                targetDisplay = playerCamera.targetDisplay;
            }
        }
        else if (player is GhostController ghost)
        {
            playerCamera = ghost.ghostCamera;
            if (playerCamera != null)
            {
                targetDisplay = playerCamera.targetDisplay;
            }
        }

        // Set canvas to target display
        canvas.targetDisplay = targetDisplay;

        // If using world space canvas, set it to follow the player's camera
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay canvas automatically appears on the correct display
            Debug.Log($"Pause menu set to display {targetDisplay}");
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (playerCamera != null)
            {
                canvas.worldCamera = playerCamera;
            }
        }

        Debug.Log($"Pause menu created on display {targetDisplay} for {player.GetType().Name}");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;

        // Resume time
        Time.timeScale = 1f;

        // Destroy the active pause menu
        if (activePauseMenu != null)
        {
            Destroy(activePauseMenu);
            activePauseMenu = null;
            activePauseCanvas = null;
            resumeButton = null;
            mainMenuButton = null;
        }

        // Unfreeze all players and ghosts
        UnfreezeAllPlayers();

        Debug.Log("Game Resumed");
    }

    private void FreezeAllPlayers()
    {
        // Find all explorers
        explorers.Clear();
        explorers.AddRange(FindObjectsOfType<PlayerInputHandler>());

        // Find all ghosts
        ghosts.Clear();
        ghosts.AddRange(FindObjectsOfType<GhostController>());

        // Freeze explorers
        foreach (var explorer in explorers)
        {
            if (explorer != null)
            {
                explorer.FreezeInput(true);
            }
        }

        // Freeze ghosts
        foreach (var ghost in ghosts)
        {
            if (ghost != null)
            {
                ghost.FreezeInput(true);
            }
        }
    }

    private void UnfreezeAllPlayers()
    {
        // Unfreeze explorers
        foreach (var explorer in explorers)
        {
            if (explorer != null)
            {
                explorer.FreezeInput(false);
            }
        }

        // Unfreeze ghosts - ADD PROPER INPUT RESET
        foreach (var ghost in ghosts)
        {
            if (ghost != null)
            {
                ghost.FreezeInput(false);

                // 🔥 FORCE RESET GHOST INPUT STATE
                ghost.ResetAllInputState();
            }
        }

        explorers.Clear();
        ghosts.Clear();

        Debug.Log("All players unfrozen and input reset");
    }

    private void HandleMenuNavigation()
    {
        if (Time.unscaledTime - lastNavigationTime < menuNavigationCooldown)
            return;

        // 🔥 IMPROVED: Use the pausing player's specific gamepad if possible
        Gamepad playerGamepad = GetPausingPlayerGamepad();
        if (playerGamepad == null)
        {
            // Fallback to first connected gamepad
            var gamepads = Gamepad.all;
            if (gamepads.Count == 0) return;
            playerGamepad = gamepads[0];
        }

        // 🔥 IMPROVED NAVIGATION WITH BETTER FEEDBACK
        if (playerGamepad.dpad.down.wasPressedThisFrame || playerGamepad.leftStick.down.wasPressedThisFrame)
        {
            // Move selection down
            if (resumeButton != null && mainMenuButton != null)
            {
                if (resumeButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                {
                    mainMenuButton.Select();
                    // 🔥 FORCE UI UPDATE
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(mainMenuButton.gameObject);
                    lastNavigationTime = Time.unscaledTime;
                    Debug.Log("Selected Main Menu button");
                }
            }
        }
        else if (playerGamepad.dpad.up.wasPressedThisFrame || playerGamepad.leftStick.up.wasPressedThisFrame)
        {
            // Move selection up
            if (resumeButton != null && mainMenuButton != null)
            {
                if (mainMenuButton.gameObject == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
                {
                    resumeButton.Select();
                    // 🔥 FORCE UI UPDATE
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
                    lastNavigationTime = Time.unscaledTime;
                    Debug.Log("Selected Resume button");
                }
            }
        }
        else if (playerGamepad.buttonSouth.wasPressedThisFrame) // A button on Xbox, Cross on PS
        {
            // Handle button press
            var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            if (selected == resumeButton?.gameObject)
            {
                ResumeGame();
            }
            else if (selected == mainMenuButton?.gameObject)
            {
                ReturnToMainMenu();
            }
        }

        // 🔥 ADD B BUTTON TO RESUME (COMMON CONVENTION)
        else if (playerGamepad.buttonEast.wasPressedThisFrame) // B button on Xbox, Circle on PS
        {
            ResumeGame();
        }
    }

    private Gamepad GetPausingPlayerGamepad()
    {
        if (pausingPlayer == null) return null;

        // Try to get the gamepad from the pausing player
        if (pausingPlayer is PlayerInputHandler explorer)
        {
            if (explorer.inputActions != null)
            {
                // Get the device from the input actions
                var devices = explorer.inputActions.devices;
                foreach (var device in devices)
                {
                    if (device is Gamepad gamepad)
                        return gamepad;
                }
            }
        }
        else if (pausingPlayer is GhostController ghost)
        {
            if (ghost.playerInputs != null)
            {
                // Get the device from the input actions
                var devices = ghost.playerInputs.devices;
                foreach (var device in devices)
                {
                    if (device is Gamepad gamepad)
                        return gamepad;
                }
            }
        }

        return null;
    }

    public void ReturnToMainMenu()
    {
        // Resume time before loading new scene
        Time.timeScale = 1f;

        // Destroy pause menu
        if (activePauseMenu != null)
        {
            Destroy(activePauseMenu);
        }

        // Load main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main menu scene name not set!");
        }
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset when this object is destroyed
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}