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
        explorer.TakeControl(inputSets[controllerIndex]);
        explorers.Add(explorer);
    }

    private void SpawnGhost(int controllerIndex)
    {
        Transform spawn = ghostSpawns[Mathf.Clamp(ghosts.Count, 0, ghostSpawns.Length - 1)];
        var ghost = Instantiate(ghostPrefab, spawn.position, spawn.rotation);
        ghost.AssignInput(inputSets[controllerIndex]);
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
            ghost.playerInputs.Disable();
            handler.TakeControl(ghost.playerInputs);

            // Simple release handler for explorer clone (no menu, just timer)
            // The timer in Update() will automatically call ReleaseClone when it expires
        }

        activeClones.Add(ghost, new CloneData { cloneObject = clone, timer = cloneDuration });

        Debug.Log("Explorer clone spawned - will auto-return after timer");
    }

    public void ReleaseClone(GhostController ghost)
    {
        // 🔹 Validate input
        if (ghost == null)
        {
            Debug.LogError("❌ ReleaseClone called with null GhostController!");
            return;
        }

        // 🔹 Check if this ghost has an active clone
        if (!activeClones.TryGetValue(ghost, out CloneData cloneData))
        {
            Debug.LogWarning($"⚠️ No active clone found for {ghost.name} to release.");
            return;
        }

        GameObject clone = cloneData.cloneObject;
        if (clone == null)
        {
            Debug.LogWarning("⚠️ Clone object reference is missing, cleaning up entry.");
            activeClones.Remove(ghost);
            return;
        }

        // 🔹 Retrieve the GhostController from the clone (if any)
        GhostController cloneController = clone.GetComponent<GhostController>();

        // 🔹 Disable clone control and reassign input back to the ghost
        if (cloneController != null)
        {
            cloneController.playerInputs.Disable();
        }

        ghost.gameObject.SetActive(true);
        ghost.SetVisibility(true);
        ghost.FreezeInput(false);
        ghost.playerInputs.Enable();

        // 🔹 Restore ghost’s camera
        if (ghost.ghostCamera != null)
        {
            ghost.ghostCamera.enabled = true;
            ghost.ghostCamera.gameObject.SetActive(true);
        }

        // 🔹 Clean up the clone object
        Destroy(clone);

        // 🔹 Remove from active clone list
        activeClones.Remove(ghost);

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

        // 🔹 Clear any lingering menu callbacks
        if (ghost.possessionMenu != null)
            ghost.possessionMenu.ClearCallbacks();

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
        else
        {
            Debug.LogError($"❌ Clone '{clone.name}' has no Rigidbody!");
        }

        // 🔹 Store original ghost state BEFORE modifying it
        Vector3 originalGhostPosition = ghost.transform.position;
        Quaternion originalGhostRotation = ghost.transform.rotation;

        // 🔹 Hide ghost and freeze input, but keep it active in the scene
        ghost.FreezeInput(true);
        ghost.SetVisibility(false);
        ghost.isControllingClone = true;

        // 🔹 Give inputs to the clone
        ghost.playerInputs.Disable();
        cloneController.AssignInput(ghost.playerInputs);
        cloneController.playerInputs.Enable();
        cloneController.FreezeInput(false);

        // 🔹 Preserve clone camera layout and mark as clone
        cloneController.preserveCameraSetup = true;
        cloneController.isControllingClone = true;

        // 🔹 Track active clone
        activeClones[ghost] = new CloneData
        {
            cloneObject = clone,
            timer = cloneDuration
        };

        // 🔹 Setup callback to return control to ghost - FIXED VERSION
        cloneController.OnDestroyClone = () =>
        {
            // Make sure the ghost still exists
            if (ghost != null)
            {
                // Restore ghost position and rotation
                ghost.transform.position = originalGhostPosition;
                ghost.transform.rotation = originalGhostRotation;

                // Reactivate ghost
                ghost.gameObject.SetActive(true);
                ghost.SetVisibility(true);
                ghost.FreezeInput(false);
                ghost.isControllingClone = false;

                // Re-enable ghost inputs and refresh subscriptions
                if (ghost.playerInputs != null)
                {
                    ghost.playerInputs.Enable();
                    ghost.AssignInput(ghost.playerInputs); // This re-subscribes all input handlers
                }

                // Reset ghost camera
                if (ghost.cameraPivot != null)
                {
                    ghost.cameraPivot.localPosition = Vector3.zero;
                    ghost.cameraPivot.localRotation = Quaternion.identity;
                }
                else if (ghost.cameraTransform != null)
                {
                    ghost.cameraTransform.localPosition = Vector3.zero;
                    ghost.cameraTransform.localRotation = Quaternion.identity;
                }

                // Clear the xRotation to reset look direction
                ghost.xRotation = 0f;

                // Remove from active clones
                activeClones.Remove(ghost);

                Debug.Log("✅ Successfully returned control to ghost. Ghost should be fully functional.");
            }
            else
            {
                Debug.LogError("❌ Ghost was destroyed before returning control!");
            }
        };
    }





    // Make CloneData public
    [System.Serializable]
    public class CloneData
    {
        public GameObject cloneObject;
        public float timer;
    }
}