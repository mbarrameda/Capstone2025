using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Simplified transformation system - no possession required.
/// Ghost can transform into 3 fixed objects: Explorer, Wall, and one other.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Controller Management")]
    private Dictionary<int, InputDevice> assignedControllers = new Dictionary<int, InputDevice>();

    [Header("Prefabs")]
    public PlayerInputHandler explorerPrefab;
    public GhostController ghostPrefab;

    [Header("Transformation Options - Add as many as you want")]
    [Tooltip("Each entry is one option in the transformation menu. Index 0 = first button, etc.")]
    public TransformationOption[] transformationOptions;

    [System.Serializable]
    public class TransformationOption
    {
        public string displayName = "Unnamed";
        public Sprite icon;
        public GameObject clonePrefab;
        public PossessableObject settings; // optional, for rotation/scale/camera overrides
    }

    [Header("Spawn Points")]
    public Transform[] explorerSpawns;
    public Transform[] ghostSpawns;

    [Header("Clone Settings")]
    public float fearDrainRateWhileUsingClone = 20f;

    public List<PlayerInputHandler> explorers = new List<PlayerInputHandler>();
    public List<GhostController> ghosts = new List<GhostController>();
    private List<PlayerInputs> inputSets = new List<PlayerInputs>();

    public Dictionary<GhostController, CloneData> activeClones = new Dictionary<GhostController, CloneData>();

    public static GameManager Instance { get; private set; }

    private void OnEnable()
    {
        // Subscribe to device change events
        InputSystem.onDeviceChange += OnDeviceChange;
    }
    private void OnDisable()
    {
        // Unsubscribe from device change events
        InputSystem.onDeviceChange -= OnDeviceChange;
    }
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        Debug.Log($"🎮 Device change: {device.name} - {change}");

        switch (change)
        {
            case InputDeviceChange.Added:
                OnControllerConnected(device);
                break;

            case InputDeviceChange.Removed:
                OnControllerDisconnected(device);
                break;

            case InputDeviceChange.Reconnected:
                OnControllerReconnected(device);
                break;

            case InputDeviceChange.Disconnected:
                OnControllerDisconnected(device);
                break;
        }
    }
    private void OnControllerConnected(InputDevice device)
    {
        if (!(device is Gamepad gamepad)) return;

        Debug.Log($"🎮 Controller connected: {device.name}");

        // Try to reassign to previously assigned player
        int playerIndex = FindPlayerIndexForDevice(device);

        if (playerIndex >= 0)
        {
            // Reassign to existing player
            ReassignControllerToPlayer(playerIndex, gamepad);
        }
        else
        {
            // New controller - could auto-assign if we have unassigned players
            Debug.Log($"New controller detected: {device.name}");
        }
    }
    private void OnControllerDisconnected(InputDevice device)
    {
        if (!(device is Gamepad)) return;

        Debug.Log($"🎮 Controller disconnected: {device.name}");
        // Player can still exist, just can't receive input until reconnected
    }
    private void OnControllerReconnected(InputDevice device)
    {
        if (!(device is Gamepad gamepad)) return;

        Debug.Log($"🎮 Controller reconnected: {device.name}");

        // Find which player had this controller
        int playerIndex = FindPlayerIndexForDevice(device);

        if (playerIndex >= 0)
        {
            ReassignControllerToPlayer(playerIndex, gamepad);
        }
    }

    private int FindPlayerIndexForDevice(InputDevice device)
    {
        // Check assigned controllers dictionary
        foreach (var kvp in assignedControllers)
        {
            if (kvp.Value == device)
            {
                return kvp.Key;
            }
        }

        // Check by device ID (persistent across reconnections)
        for (int i = 0; i < inputSets.Count; i++)
        {
            var devices = inputSets[i].devices;

            if (devices.HasValue && devices.Value.Count > 0)
            {
                var firstDevice = devices.Value[0];
                if (firstDevice != null && firstDevice.deviceId == device.deviceId)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private void ReassignControllerToPlayer(int playerIndex, Gamepad gamepad)
    {
        Debug.Log($"🔄 Reassigning controller to player {playerIndex}");

        if (playerIndex >= inputSets.Count)
        {
            Debug.LogError($"Invalid player index: {playerIndex}");
            return;
        }

        // Update the input set with new device reference
        inputSets[playerIndex].devices = new InputDevice[] { gamepad };

        // Reassign to explorer or ghost
        if (playerIndex < explorers.Count)
        {
            // Explorer
            explorers[playerIndex].ReleaseControl();
            explorers[playerIndex].TakeControl(inputSets[playerIndex]);
            Debug.Log($"✅ Reassigned controller to Explorer {playerIndex}");
        }
        else if (playerIndex - explorers.Count < ghosts.Count)
        {
            // Ghost
            int ghostIndex = playerIndex - explorers.Count;
            ghosts[ghostIndex].RemoveInput();
            ghosts[ghostIndex].AssignInput(inputSets[playerIndex]);
            Debug.Log($"✅ Reassigned controller to Ghost {ghostIndex}");
        }

        // Update tracking
        assignedControllers[playerIndex] = gamepad;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetupPlayers();
    }

    private void Update()
    {
        List<GhostController> finishedClones = new List<GhostController>();

        foreach (var kvp in activeClones)
        {
            GhostController ghost = kvp.Key;
            CloneData data = kvp.Value;

            // Drain fear while transformed
            ghost.fear -= fearDrainRateWhileUsingClone * Time.deltaTime;
            ghost.fear = Mathf.Max(ghost.fear, 0f);

            if (ghost.fear <= 0f)
            {
                Debug.Log("⚠️ Fear depleted - releasing transformation");
                finishedClones.Add(ghost);
            }
        }

        foreach (var ghost in finishedClones)
            ReleaseClone(ghost);
    }
    public void ManuallyReleaseTransform(GhostController ghost)
    {
        if (ghost == null)
        {
            Debug.LogError("❌ ManuallyReleaseTransform called with null ghost!");
            return;
        }

        if (!HasActiveClone(ghost))
        {
            Debug.LogWarning($"⚠️ {ghost.name} is not transformed");
            return;
        }

        Debug.Log("🔵 Manual transformation release requested");
        ReleaseClone(ghost);
    }

    private void SetupPlayers()
    {
        int controllerCount = Gamepad.all.Count;
        if (controllerCount < 2)
        {
            Debug.LogError("2 controllers required for split-screen!");
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            var input = new PlayerInputs();
            input.devices = new InputDevice[] { Gamepad.all[i] };
            inputSets.Add(input);

            assignedControllers[i] = Gamepad.all[i];
        }

        SpawnExplorer(0);
        SpawnGhost(1);
    }

    private void SpawnExplorer(int controllerIndex)
    {
        Transform spawn = explorerSpawns[Mathf.Clamp(explorers.Count, 0, explorerSpawns.Length - 1)];
        var explorer = Instantiate(explorerPrefab, spawn.position, spawn.rotation);

        var explorerInputs = new PlayerInputs();
        explorerInputs.devices = new InputDevice[] { Gamepad.all[controllerIndex] };

        explorerInputs.Player.Enable();
        explorerInputs.Ghost.Disable();

        explorer.TakeControl(explorerInputs);
        explorer.SetSanityActive(true);
        explorers.Add(explorer);
    }

    private void SpawnGhost(int controllerIndex)
    {
        Transform spawn = ghostSpawns[Mathf.Clamp(ghosts.Count, 0, ghostSpawns.Length - 1)];
        var ghost = Instantiate(ghostPrefab, spawn.position, spawn.rotation);

        var ghostInputs = new PlayerInputs();
        ghostInputs.devices = new InputDevice[] { Gamepad.all[controllerIndex] };

        ghostInputs.Ghost.Enable();
        ghostInputs.Player.Disable();

        ghost.AssignInput(ghostInputs);
        ghosts.Add(ghost);
    }

    // ==================== NEW SIMPLIFIED TRANSFORMATION SYSTEM ====================

    // ==================== TRANSFORMATION SYSTEM ====================

    /// <summary>
    /// Transform into whichever option is at this index in transformationOptions[]
    /// </summary>
    public void TransformIntoOption(GhostController ghost, int index)
    {
        if (transformationOptions == null || index < 0 || index >= transformationOptions.Length)
        {
            Debug.LogError($"TransformIntoOption: index {index} is out of range (options count: {transformationOptions?.Length ?? 0})");
            return;
        }

        var option = transformationOptions[index];
        TransformInto(ghost, option.clonePrefab, option.settings, option.displayName);
    }

    private void TransformInto(GhostController ghost, GameObject prefab, PossessableObject settings, string name)
    {
        Debug.Log($"🔵 Transforming into {name}...");

        if (ghost == null)
        {
            Debug.LogError("❌ GhostController is null!");
            return;
        }

        if (activeClones.ContainsKey(ghost))
        {
            Debug.Log("Ghost already has an active clone");
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"No {name} clone prefab assigned!");
            return;
        }

        // Use TransformApplier to apply the visual appearance
        TransformApplier.Apply(ghost.gameObject, prefab, settings);

        // Mark as controlling clone
        ghost.isControllingClone = true;

        Light ghostLight = ghost.GetComponentInChildren<Light>();
        if (ghostLight != null)
        {
            ghostLight.enabled = false;
            Debug.Log("💡 Ghost light disabled");
        }

        // Create clone data
        CloneData cloneData = new CloneData();
        activeClones[ghost] = cloneData;

        // Apply camera settings if available
        Camera cloneCamera = null;
        foreach (Camera cam in ghost.GetComponentsInChildren<Camera>())
        {
            if (cam == ghost.ghostCamera) continue;
            cloneCamera = cam;
            break;
        }

        if (cloneCamera != null)
        {
            // Stamp the correct split-screen rect and disable it BEFORE it renders even one frame
            if (ghost.ghostCamera != null)
                cloneCamera.rect = ghost.ghostCamera.rect;

            cloneCamera.enabled = false;

            // Now do the proper swap
            ghost.ActivateCloneCamera(cloneCamera);
        }
        else
        {
            Debug.LogWarning($"⚠️ No camera found in clone prefab for {name}. Ghost camera will stay active.");
        }
    }

    /// <summary>
    /// Release transformation and return to ghost form
    /// </summary>
    public void ReleaseClone(GhostController ghost)
    {
        if (ghost == null)
        {
            Debug.LogError("❌ ReleaseClone called with null GhostController!");
            return;
        }

        if (!activeClones.TryGetValue(ghost, out CloneData cloneData))
        {
            Debug.LogWarning($"⚠️ No active clone found for {ghost.name}");
            return;
        }

        Debug.Log("🔵 Releasing transformation...");

        // Use TransformApplier to revert visual appearance
        TransformApplier.Revert(ghost.gameObject);

        // Reset ghost state
        ghost.gameObject.SetActive(true);
        ghost.SetVisibility(true);
        ghost.FreezeInput(false);
        ghost.isControllingClone = false;

        Light ghostLight = ghost.GetComponentInChildren<Light>();
        if (ghostLight != null)
        {
            ghostLight.enabled = true;
            Debug.Log("💡 Ghost light re-enabled");
        }

        ghost.DeactivateCloneCamera();

        activeClones.Remove(ghost);
        Debug.Log("✅ Transformation released - back to ghost form");
    }

    public bool HasActiveClone(GhostController ghost)
    {
        return activeClones.ContainsKey(ghost);
    }

    /// <summary>
    /// Returns the display name and icon for each transformation option, for building the menu.
    /// </summary>
    public (string name, Sprite icon)[] GetTransformationMenuData()
    {
        if (transformationOptions == null) return System.Array.Empty<(string, Sprite)>();

        var result = new (string name, Sprite icon)[transformationOptions.Length];
        for (int i = 0; i < transformationOptions.Length; i++)
            result[i] = (transformationOptions[i].displayName, transformationOptions[i].icon);

        return result;
    }
    [System.Serializable]
    public class CloneData
    {
    }

}