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

    // ------------------ Transform into Explorer (Mesh Swap) ------------------ //

    public void SpawnPossessedClone(GhostController ghost, PossessableObject obj)
    {
        Debug.Log("🟢 Transforming into EXPLORER...");

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

        if (explorerClonePrefab == null)
        {
            Debug.LogWarning("No explorer clone prefab assigned!");
            return;
        }

        // Store original ghost appearance
        CloneData cloneData = StoreGhostAppearance(ghost);
        cloneData.timer = cloneDuration;

        // Get explorer appearance from the prefab
        MeshFilter explorerMeshFilter = explorerClonePrefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer explorerMeshRenderer = explorerClonePrefab.GetComponentInChildren<MeshRenderer>();

        if (explorerMeshFilter == null || explorerMeshRenderer == null)
        {
            Debug.LogError("❌ Explorer prefab is missing MeshFilter or MeshRenderer!");
            return;
        }

        // Apply explorer appearance to ghost
        ApplyAppearanceToGhost(ghost, explorerMeshFilter.sharedMesh, explorerMeshRenderer.sharedMaterial);

        // Mark as controlling clone
        ghost.isControllingClone = true;

        // Switch to Player controls
        if (ghost.playerInputs != null)
        {
            ghost.playerInputs.Ghost.Disable();
            ghost.playerInputs.Player.Enable();
        }

        // Track active transformation
        activeClones[ghost] = cloneData;

        Debug.Log("✅ Transformed into Explorer!");
    }

    // ------------------ Transform into Object (Mesh Swap) ------------------ //

    public void SpawnObjectClone(GhostController ghost, PossessableObject obj)
    {
        Debug.Log("🔵 Transforming into OBJECT...");

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

        if (obj == null || obj.clonePrefab == null)
        {
            Debug.LogWarning("No valid disguise prefab");
            return;
        }

        Debug.Log($"🔵 Transforming into: {obj.clonePrefab.name}");

        // Store original ghost appearance
        CloneData cloneData = StoreGhostAppearance(ghost);
        cloneData.timer = cloneDuration;

        // Get object appearance from the prefab
        MeshFilter objectMeshFilter = obj.clonePrefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer objectMeshRenderer = obj.clonePrefab.GetComponentInChildren<MeshRenderer>();

        if (objectMeshFilter == null || objectMeshRenderer == null)
        {
            Debug.LogError("❌ Object prefab is missing MeshFilter or MeshRenderer!");
            Debug.LogError($"Prefab structure: {obj.clonePrefab.name}");

            // Debug: print all components
            foreach (var component in obj.clonePrefab.GetComponentsInChildren<Component>())
            {
                Debug.Log($"  - {component.GetType().Name} on {component.gameObject.name}");
            }
            return;
        }

        // Apply object appearance to ghost
        ApplyAppearanceToGhost(ghost, objectMeshFilter.sharedMesh, objectMeshRenderer.sharedMaterial);

        // Mark as controlling clone
        ghost.isControllingClone = true;

        // Track active transformation
        activeClones[ghost] = cloneData;

        Debug.Log($"✅ Transformed into {obj.displayName}!");
    }

    // ------------------ Helper Methods for Mesh Swapping ------------------ //

    private CloneData StoreGhostAppearance(GhostController ghost)
    {
        MeshFilter ghostMeshFilter = ghost.GetComponentInChildren<MeshFilter>();
        MeshRenderer ghostMeshRenderer = ghost.GetComponentInChildren<MeshRenderer>();
        Collider ghostCollider = ghost.GetComponent<Collider>();

        CloneData cloneData = new CloneData
        {
            cloneObject = null, // No actual clone object for mesh swap
            originalMesh = ghostMeshFilter != null ? ghostMeshFilter.sharedMesh : null,
            originalMaterials = ghostMeshRenderer != null ? ghostMeshRenderer.sharedMaterials : null,
            originalColliderType = ghostCollider != null ? ghostCollider.GetType().Name : null
        };

        // Store collider properties based on type
        if (ghostCollider is BoxCollider boxCol)
        {
            cloneData.boxCenter = boxCol.center;
            cloneData.boxSize = boxCol.size;
        }
        else if (ghostCollider is SphereCollider sphereCol)
        {
            cloneData.sphereCenter = sphereCol.center;
            cloneData.sphereRadius = sphereCol.radius;
        }
        else if (ghostCollider is CapsuleCollider capsuleCol)
        {
            cloneData.capsuleCenter = capsuleCol.center;
            cloneData.capsuleRadius = capsuleCol.radius;
            cloneData.capsuleHeight = capsuleCol.height;
            cloneData.capsuleDirection = capsuleCol.direction;
        }

        return cloneData;
    }

    private void ApplyAppearanceToGhost(GhostController ghost, Mesh newMesh, Material newMaterial)
    {
        MeshFilter ghostMeshFilter = ghost.GetComponentInChildren<MeshFilter>();
        MeshRenderer ghostMeshRenderer = ghost.GetComponentInChildren<MeshRenderer>();

        if (ghostMeshFilter != null && newMesh != null)
        {
            ghostMeshFilter.sharedMesh = newMesh;
            Debug.Log($"✅ Applied mesh: {newMesh.name}");
        }
        else
        {
            Debug.LogError("❌ Failed to apply mesh!");
        }

        if (ghostMeshRenderer != null && newMaterial != null)
        {
            ghostMeshRenderer.sharedMaterial = newMaterial;
            Debug.Log($"✅ Applied material: {newMaterial.name}");
        }
        else
        {
            Debug.LogError("❌ Failed to apply material!");
        }
    }

    // ------------------ Release Clone (Restore Original Appearance) ------------------ //

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

        // Restore original ghost appearance
        MeshFilter ghostMeshFilter = ghost.GetComponentInChildren<MeshFilter>();
        MeshRenderer ghostMeshRenderer = ghost.GetComponentInChildren<MeshRenderer>();
        Collider ghostCollider = ghost.GetComponent<Collider>();

        if (ghostMeshFilter != null && cloneData.originalMesh != null)
        {
            ghostMeshFilter.sharedMesh = cloneData.originalMesh;
            Debug.Log("✅ Restored original mesh");
        }

        if (ghostMeshRenderer != null && cloneData.originalMaterials != null)
        {
            ghostMeshRenderer.sharedMaterials = cloneData.originalMaterials;
            Debug.Log("✅ Restored original materials");
        }

        // Restore original collider shape
        if (ghostCollider != null && !string.IsNullOrEmpty(cloneData.originalColliderType))
        {
            if (cloneData.originalColliderType == "BoxCollider" && ghostCollider is BoxCollider ghostBox)
            {
                ghostBox.center = cloneData.boxCenter;
                ghostBox.size = cloneData.boxSize;
            }
            else if (cloneData.originalColliderType == "SphereCollider" && ghostCollider is SphereCollider ghostSphere)
            {
                ghostSphere.center = cloneData.sphereCenter;
                ghostSphere.radius = cloneData.sphereRadius;
            }
            else if (cloneData.originalColliderType == "CapsuleCollider" && ghostCollider is CapsuleCollider ghostCapsule)
            {
                ghostCapsule.center = cloneData.capsuleCenter;
                ghostCapsule.radius = cloneData.capsuleRadius;
                ghostCapsule.height = cloneData.capsuleHeight;
                ghostCapsule.direction = cloneData.capsuleDirection;
            }
            Debug.Log("✅ Restored original collider");
        }

        // Reset ghost state
        ghost.gameObject.SetActive(true);
        ghost.SetVisibility(true);
        ghost.FreezeInput(false);
        ghost.isControllingClone = false;

        // Re-enable ghost input
        if (ghost.playerInputs != null)
        {
            ghost.playerInputs.Player.Disable();
            ghost.playerInputs.Ghost.Enable();
            ghost.AssignInput(ghost.playerInputs);
        }

        activeClones.Remove(ghost);
        Debug.Log("✅ Transformation released - back to ghost form");
    }

    public bool HasActiveClone(GhostController ghost)
    {
        return activeClones.ContainsKey(ghost);
    }

    // Make CloneData public with expanded fields for storing appearance
    [System.Serializable]
    public class CloneData
    {
        public GameObject cloneObject;
        public float timer;

        // Store original appearance for mesh swapping
        public Mesh originalMesh;
        public Material[] originalMaterials;

        // Store collider info
        public string originalColliderType;

        // BoxCollider
        public Vector3 boxCenter;
        public Vector3 boxSize;

        // SphereCollider
        public Vector3 sphereCenter;
        public float sphereRadius;

        // CapsuleCollider
        public Vector3 capsuleCenter;
        public float capsuleRadius;
        public float capsuleHeight;
        public int capsuleDirection;
    }
}