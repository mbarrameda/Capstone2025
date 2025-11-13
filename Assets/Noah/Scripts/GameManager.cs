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

    private List<PlayerInputHandler> explorers = new List<PlayerInputHandler>();
    private List<GhostController> ghosts = new List<GhostController>();
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
        SetupDisplays();
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

    private void SetupDisplays()
    {
        if (Display.displays.Length > 1)
        {
            Display.displays[0].Activate();
            Display.displays[1].Activate();
        }

        for (int i = 0; i < explorers.Count; i++)
        {
            Camera cam = explorers[i].playerCamera;
            cam.targetDisplay = 0;
            cam.rect = explorers.Count == 1 ? new Rect(0, 0, 1, 1) : i == 0 ? new Rect(0, 0.5f, 1, 0.5f) : new Rect(0, 0, 1, 0.5f);
        }

        for (int i = 0; i < ghosts.Count; i++)
        {
            Camera cam = ghosts[i].ghostCamera;
            cam.targetDisplay = 1;
            cam.rect = ghosts.Count == 1 ? new Rect(0, 0, 1, 1) : i == 0 ? new Rect(0, 0.5f, 1, 0.5f) : new Rect(0, 0, 1, 0.5f);
        }
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
        if (handler != null)
        {
            ghost.playerInputs.Ghost.Disable();

            // Transfer inputs to clone
            handler.TakeControl(ghost.playerInputs);

            ghost.playerInputs.Player.Enable();
        }

        activeClones.Add(ghost, new CloneData { cloneObject = clone, timer = cloneDuration });
    }

    public void ReleaseClone(GhostController ghost)
    {

        if (ghost != null)
        {
            ghost.ReturnControlToGhost();
        }

        if (ghost == null)
        {
            Debug.LogError("❌ ReleaseClone called with null GhostController!");
            return;
        }

        if (!activeClones.TryGetValue(ghost, out CloneData cloneData))
        {
            Debug.LogWarning($"⚠️ No active clone found for {ghost.name} to release.");
            return;
        }

        GameObject clone = cloneData.cloneObject;
        if (clone != null)
        {
            // 🔥 Clean up clone inputs first
            var handler = clone.GetComponent<PlayerInputHandler>();
            if (handler != null)
            {
                handler.ReleaseControl(); // Use ReleaseControl instead of RemoveInput
            }
            Destroy(clone);
        }

        // Re-enable ghost control and camera
        ghost.gameObject.SetActive(true);
        ghost.SetVisibility(true);
        ghost.FreezeInput(false);
        ghost.isControllingClone = false;

        // 🧩 FIXED: Store reference before removing input
        var storedInputs = ghost.playerInputs;

        if (ghost.playerInputs != null)
        {
            // Make sure only the Ghost action map is active
            ghost.playerInputs.Player.Disable();
            ghost.playerInputs.Ghost.Enable();

            // Rebind input callbacks fresh
            ghost.RemoveInput();
            ghost.AssignInput(ghost.playerInputs);

            // Double-check
            Debug.Log($"✅ Rebound ghost inputs. Ghost map enabled: {ghost.playerInputs.Ghost.enabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ Ghost had no PlayerInputs when returning from clone. Creating new one.");
            var newInputs = new PlayerInputs();
            newInputs.Ghost.Enable();
            ghost.AssignInput(newInputs);
        }

        // Reactivate camera if it got disabled
        if (ghost.ghostCamera != null)
        {
            ghost.ghostCamera.enabled = true;
            ghost.ghostCamera.gameObject.SetActive(true);
        }

        activeClones.Remove(ghost);
        ghost.FreezeInput(false);
        Debug.Log($"✅ Clone released. Control returned to {ghost.name}");

    }


    public bool HasActiveClone(GhostController ghost)
    {
        return activeClones.ContainsKey(ghost);
    }

    public void SpawnObjectClone(GhostController ghost, PossessableObject obj)
    {
        // 🔹 Sanity check
        if (ghost == null)
        {
            Debug.LogError("❌ GhostController is null in SpawnObjectClone!");
            return;
        }

        // 🔹 Prevent multiple clones
        if (activeClones.ContainsKey(ghost))
        {
            Debug.Log("Ghost already has an active clone");
            return;
        }

        // 🔹 If no object, spawn explorer clone instead
        if (obj == null || obj.clonePrefab == null)
        {
            SpawnExplorerClone(ghost);
            return;
        }

        // 🔹 Store original ghost state BEFORE modifying it
        Vector3 originalGhostPosition = ghost.transform.position;
        Quaternion originalGhostRotation = ghost.transform.rotation;
        bool originalGhostVisibility = ghost.ghostRenderer?.enabled ?? false;

        // 🔹 Instantiate object clone at ghost position
        GameObject clone = Instantiate(
            obj.clonePrefab,
            ghost.transform.position + cloneOffset,
            ghost.transform.rotation
        );

        GhostController cloneController = clone.GetComponent<GhostController>();
        if (cloneController == null)
        {
            Debug.LogError($"❌ Clone '{clone.name}' has no GhostController component!");
            Destroy(clone);
            return;
        }

        // 🔹 Setup Rigidbody if missing
        cloneController.rb = cloneController.GetComponent<Rigidbody>();
        if (cloneController.rb != null)
        {
            cloneController.rb.isKinematic = false;
            cloneController.rb.constraints = RigidbodyConstraints.FreezeRotation;
            cloneController.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // 🔹 Hide ghost but keep it active and functional
        ghost.FreezeInput(true);
        ghost.SetVisibility(false);
        ghost.isControllingClone = true;

        // 🔥 CRITICAL FIX: Proper input transfer for object clones
        if (ghost.playerInputs != null)
        {
            // Disable ghost's Ghost action map before transfer
            ghost.playerInputs.Ghost.Disable();

            // Transfer inputs to clone
            ghost.playerInputs.Disable(); // Disable all first
            cloneController.AssignInput(ghost.playerInputs);

            // 🔥 Ensure object clone uses Ghost action map (not Explorer)
            ghost.playerInputs.Ghost.Enable();
            ghost.playerInputs.Player.Disable();
        }

        cloneController.FreezeInput(false);
        cloneController.isControllingClone = true;
        cloneController.preserveCameraSetup = true;

        // 🔹 Track active clone
        activeClones[ghost] = new CloneData
        {
            cloneObject = clone,
            timer = cloneDuration
        };

        // 🔹 FIXED: Setup callback to return control to ghost
        cloneController.OnDestroyClone = () =>
        {
            if (ghost != null && ghost.gameObject != null)
            {
                Debug.Log("🚨 NUCLEAR RESET - Returning from object clone");

                // 1. Store the original input devices before we lose them
                InputDevice[] deviceArray = InputSystem.devices.ToArray();

                // 2. Completely nuke the current input system
                if (ghost.playerInputs != null)
                {
                    ghost.playerInputs.Disable();
                    ghost.RemoveInput();
                    ghost.playerInputs = null; // Complete destruction
                }

                // 3. Restore ghost transform and visibility
                ghost.transform.position = originalGhostPosition;
                ghost.transform.rotation = originalGhostRotation;
                ghost.gameObject.SetActive(true);
                ghost.SetVisibility(true);
                ghost.isControllingClone = false;

                // 4. EMERGENCY RESET all input state
                ghost.EmergencyInputReset();

                // 5. Create BRAND NEW input system from scratch
                if (deviceArray != null && deviceArray.Length > 0)
                {
                    var freshInputs = new PlayerInputs();
                    freshInputs.devices = deviceArray;

                    // Only enable Ghost action map
                    freshInputs.Ghost.Enable();
                    freshInputs.Player.Disable();

                    // Assign fresh inputs
                    ghost.AssignInput(freshInputs);
                }
                else
                {
                    Debug.LogError("❌ No input devices found for ghost restoration!");
                    // Fallback: create new inputs with default devices
                    ghost.AssignInput(new PlayerInputs());
                }

                // 6. Ensure input is unfrozen
                ghost.FreezeInput(false);

                // 7. Remove from active clones
                activeClones.Remove(ghost);

                Debug.Log("✅ Nuclear reset complete - Ghost should be fully functional");
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