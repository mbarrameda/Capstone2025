using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public PlayerInputHandler explorerPrefab;
    public GhostController ghostPrefab;
    public GameObject explorerClonePrefab;

    [Header("Spawn Points")]
    public Transform[] explorerSpawns;
    public Transform[] ghostSpawns;

    [Header("Clone Settings")]
    public Vector3 cloneOffset = new Vector3(1.5f, 0f, 0f);
    public float fearDrainRateWhileUsingClone = 20f;
    public float cloneDuration = 15f;
    public float cloneFearCost = 25f;

    public List<PlayerInputHandler> explorers = new List<PlayerInputHandler>();
    public List<GhostController> ghosts = new List<GhostController>();
    private List<PlayerInputs> inputSets = new List<PlayerInputs>();

    // Change from private to public and make CloneData public
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

    // ------------------ Player & Ghost Setup ------------------ //

    private void SetupPlayers()
    {
        int controllerCount = Gamepad.all.Count;
        if (controllerCount < 2)
        {
            Debug.LogError("At least 2 controllers required!");
            return;
        }

        for (int i = 0; i < controllerCount; i++)
        {
            var input = new PlayerInputs();
            input.devices = new InputDevice[] { Gamepad.all[i] };
            inputSets.Add(input);
        }

        switch (controllerCount)
        {
            case 2:
                SpawnExplorer(0);
                SpawnGhost(1);
                break;
            case 3:
                SpawnExplorer(0);
                SpawnExplorer(1);
                SpawnGhost(2);
                break;
            default:
                SpawnExplorer(0);
                SpawnExplorer(1);
                SpawnGhost(2);
                SpawnGhost(3);
                break;
        }
    }

    private void SpawnExplorer(int controllerIndex)
    {
        Transform spawn = explorerSpawns[Mathf.Clamp(explorers.Count, 0, explorerSpawns.Length - 1)];
        var explorer = Instantiate(explorerPrefab, spawn.position, spawn.rotation);

        // Create COMPLETELY SEPARATE input actions for explorer
        var explorerInputs = new PlayerInputs();
        explorerInputs.devices = new InputDevice[] { Gamepad.all[controllerIndex] };

        // 🔥 CRITICAL: Make sure explorer uses Explorer action map
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

        // Create COMPLETELY SEPARATE input actions for ghost
        var ghostInputs = new PlayerInputs();
        ghostInputs.devices = new InputDevice[] { Gamepad.all[controllerIndex] };

        // 🔥 CRITICAL: Make sure ghost uses Ghost action map
        ghostInputs.Ghost.Enable();
        ghostInputs.Player.Disable();

        ghost.AssignInput(ghostInputs);
        ghosts.Add(ghost);
    }

    // ------------------ Clone Handling ------------------ //

    public void SpawnPossessedClone(GhostController ghost, PossessableObject obj)
    {
        if (obj == null || obj.clonePrefab == null)
        {
            Debug.Log("Spawning explorer clone instead.");
            SpawnExplorerClone(ghost);
            return;
        }

        GameObject clone = Instantiate(obj.clonePrefab, ghost.transform.position + cloneOffset, ghost.transform.rotation);

        ghost.FreezeInput(true);
        ghost.SetVisibility(false);

        var handler = clone.GetComponent<PlayerInputHandler>();
        if (handler != null)
        {
            ghost.playerInputs.Disable();
            handler.TakeControl(ghost.playerInputs);
        }

        activeClones.Add(ghost, new CloneData { cloneObject = clone, timer = cloneDuration });
    }

    public void SpawnExplorerClone(GhostController ghost)
    {
        GameObject clone = Instantiate(explorerClonePrefab, ghost.transform.position + cloneOffset, ghost.transform.rotation);

        ghost.FreezeInput(true);
        ghost.SetVisibility(false);
        ghost.isControllingClone = true;

        var handler = clone.GetComponent<PlayerInputHandler>();
        if (handler != null && ghost.playerInputs != null)
        {
            // 🔥 CRITICAL FIX: Create a SEPARATE input instance for the clone
            var cloneInputs = new PlayerInputs();
            cloneInputs.devices = ghost.playerInputs.devices; // Copy devices from ghost

            // Clone uses Player action map
            cloneInputs.Player.Enable();
            cloneInputs.Ghost.Disable();

            handler.SetAsClone();
            handler.TakeControl(cloneInputs);

            handler.SetSanityActive(false);
            // 🔥 Ghost retains its own input instance with Ghost map enabled
            ghost.playerInputs.Ghost.Enable();
            ghost.playerInputs.Player.Disable();
        }

        activeClones.Add(ghost, new CloneData { cloneObject = clone, timer = cloneDuration });
    }

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

        GameObject clone = cloneData.cloneObject;
        if (clone != null)
        {
            // Clean up clone inputs
            var handler = clone.GetComponent<PlayerInputHandler>();
            var cloneController = clone.GetComponent<GhostController>();

            if (handler != null)
            {
                handler.SetSanityActive(false);
                handler.ReleaseControl();
            }
            if (cloneController != null)
            {
                cloneController.RemoveInput();
            }

            Destroy(clone);
        }

        // Restore ghost
        ghost.gameObject.SetActive(true);
        ghost.SetVisibility(true);
        ghost.FreezeInput(false);
        ghost.isControllingClone = false;

        // 🔥 CRITICAL: Ensure ghost input is properly restored
        if (ghost.playerInputs != null)
        {
            ghost.playerInputs.Player.Disable();
            ghost.playerInputs.Ghost.Enable();
            ghost.AssignInput(ghost.playerInputs);
        }

        activeClones.Remove(ghost);
        Debug.Log($"✅ Clone released. Control returned to {ghost.name}");
    }

    public bool HasActiveClone(GhostController ghost)
    {
        return activeClones.ContainsKey(ghost);
    }

    public void SpawnObjectClone(GhostController ghost, PossessableObject obj)
    {
        ghost.isControllingClone = true;

        if (ghost == null)
        {
            Debug.LogError("❌ GhostController is null in SpawnObjectClone!");
            return;
        }

        if (activeClones.ContainsKey(ghost))
        {
            Debug.Log("Ghost already has an active clone");
            return;
        }

        if (obj == null || obj.clonePrefab == null)
        {
            SpawnExplorerClone(ghost);
            return;
        }

        GameObject clone = Instantiate(obj.clonePrefab, ghost.transform.position + cloneOffset, ghost.transform.rotation);

        GhostController cloneController = clone.GetComponent<GhostController>();
        if (cloneController == null)
        {
            Debug.LogError($"❌ Clone '{clone.name}' has no GhostController component!");
            Destroy(clone);
            return;
        }

        // Setup clone
        cloneController.rb = cloneController.GetComponent<Rigidbody>();
        if (cloneController.rb != null)
        {
            cloneController.rb.isKinematic = false;
            cloneController.rb.constraints = RigidbodyConstraints.FreezeRotation;
            cloneController.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Hide ghost but keep it active
        ghost.FreezeInput(true);
        ghost.SetVisibility(false);
        ghost.isControllingClone = true;

        // 🔥 CRITICAL FIX: Create SEPARATE input for object clone
        if (ghost.playerInputs != null)
        {
            var cloneInputs = new PlayerInputs();
            cloneInputs.devices = ghost.playerInputs.devices;

            // Object clone uses Ghost action map (since it's still a ghost-type entity)
            cloneInputs.Ghost.Enable();
            cloneInputs.Player.Disable();

            cloneController.AssignInput(cloneInputs);
        }

        cloneController.FreezeInput(false);
        cloneController.isControllingClone = true;
        cloneController.preserveCameraSetup = true;

        // Track active clone
        activeClones[ghost] = new CloneData
        {
            cloneObject = clone,
            timer = cloneDuration
        };

        // Setup return callback
        cloneController.OnDestroyClone = () =>
        {
            if (ghost != null)
            {
                Debug.Log("Returning from object clone");

                // Restore ghost
                ghost.gameObject.SetActive(true);
                ghost.SetVisibility(true);
                ghost.isControllingClone = false;
                ghost.FreezeInput(false);

                // 🔥 CRITICAL: Re-enable ghost's original input
                if (ghost.playerInputs != null)
                {
                    ghost.playerInputs.Ghost.Enable();
                    ghost.playerInputs.Player.Disable();
                    ghost.AssignInput(ghost.playerInputs);
                }

                // Remove from active clones
                activeClones.Remove(ghost);
            }
        };
    }

    private System.Collections.IEnumerator DelayedInputReassign(GhostController ghost, PlayerInputs storedInputs)
    {
        yield return null; // Wait one frame

        if (ghost != null && storedInputs != null)
        {
            // Ensure proper action map state
            storedInputs.Ghost.Enable();
            storedInputs.Player.Disable();

            // Reassign inputs
            ghost.AssignInput(storedInputs);
            ghost.FreezeInput(false); // Ensure input is not frozen

            Debug.Log("✅ Ghost input system fully restored after frame delay");
        }
    }

    // Make CloneData public
    [System.Serializable]
    public class CloneData
    {
        public GameObject cloneObject;
        public float timer;
    }
}