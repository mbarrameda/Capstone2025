using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Simplified transformation system - no possession required.
/// Ghost can transform into 3 fixed objects: Explorer, Wall, and one other.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public PlayerInputHandler explorerPrefab;
    public GhostController ghostPrefab;

    [Header("Transformation Prefabs - Always Available")]
    public GameObject explorerClonePrefab;
    public GameObject wallClonePrefab;
    public GameObject otherClonePrefab; // Your third transformable object

    [Header("PossessableObject Components (for settings)")]
    [Tooltip("Attach a PossessableObject component to these to configure rotation/scale/camera")]
    public PossessableObject explorerSettings;
    public PossessableObject wallSettings;
    public PossessableObject otherSettings;

    [Header("Spawn Points")]
    public Transform[] explorerSpawns;
    public Transform[] ghostSpawns;

    [Header("Clone Settings")]
    public float fearDrainRateWhileUsingClone = 20f;
    public float cloneDuration = 15f;

    public List<PlayerInputHandler> explorers = new List<PlayerInputHandler>();
    public List<GhostController> ghosts = new List<GhostController>();
    private List<PlayerInputs> inputSets = new List<PlayerInputs>();

    public Dictionary<GhostController, CloneData> activeClones = new Dictionary<GhostController, CloneData>();

    public static GameManager Instance { get; private set; }

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

            ghost.fear -= fearDrainRateWhileUsingClone * Time.deltaTime;
            ghost.fear = Mathf.Max(ghost.fear, 0f);
            data.timer -= Time.deltaTime;

            if (data.timer <= 0f || ghost.fear <= 0f)
                finishedClones.Add(ghost);
        }

        foreach (var ghost in finishedClones)
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

    /// <summary>
    /// Transform into Explorer
    /// </summary>
    public void TransformIntoExplorer(GhostController ghost)
    {
        TransformInto(ghost, explorerClonePrefab, explorerSettings, "Explorer");
    }

    /// <summary>
    /// Transform into Wall
    /// </summary>
    public void TransformIntoWall(GhostController ghost)
    {
        TransformInto(ghost, wallClonePrefab, wallSettings, "Wall");
    }

    /// <summary>
    /// Transform into Other Object
    /// </summary>
    public void TransformIntoOther(GhostController ghost)
    {
        TransformInto(ghost, otherClonePrefab, otherSettings, "Other");
    }

    /// <summary>
    /// Generic transformation method
    /// </summary>
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

        // Create clone data
        CloneData cloneData = new CloneData();
        cloneData.timer = cloneDuration;
        activeClones[ghost] = cloneData;

        // Apply camera settings if available
        TransformationCameraController cameraController = ghost.GetComponent<TransformationCameraController>();
        if (cameraController != null && settings != null)
        {
            settings.ApplyCameraSettings(cameraController);
        }

        Debug.Log($"✅ Transformed into {name}!");
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

        // Reset camera to first person
        TransformationCameraController cameraController = ghost.GetComponent<TransformationCameraController>();
        if (cameraController != null)
        {
            cameraController.ForceFirstPerson();
        }

        activeClones.Remove(ghost);
        Debug.Log("✅ Transformation released - back to ghost form");
    }

    public bool HasActiveClone(GhostController ghost)
    {
        return activeClones.ContainsKey(ghost);
    }

    [System.Serializable]
    public class CloneData
    {
        public float timer;
    }
}