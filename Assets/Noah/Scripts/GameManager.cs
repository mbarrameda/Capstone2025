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

        var handler = clone.GetComponent<PlayerInputHandler>();
        if (handler != null)
        {
            ghost.playerInputs.Disable();
            handler.TakeControl(ghost.playerInputs);
        }

        activeClones.Add(ghost, new CloneData { cloneObject = clone, timer = cloneDuration });
    }

    private void ReleaseClone(GhostController ghost)
    {
        if (!activeClones.ContainsKey(ghost)) return;

        var data = activeClones[ghost];
        if (data.cloneObject != null) Destroy(data.cloneObject);

        ghost.FreezeInput(false);
        ghost.SetVisibility(true);
        ghost.playerInputs.Enable();
        ghost.AssignInput(ghost.playerInputs);

        activeClones.Remove(ghost);
    }

    // Add this public method to check if a ghost has an active clone
    public bool HasActiveClone(GhostController ghost)
    {
        return activeClones.ContainsKey(ghost);
    }

    public void SpawnObjectClone(GhostController ghost, PossessableObject obj)
    {
        if (activeClones.ContainsKey(ghost)) return; // already controlling a clone

        if (obj.clonePrefab == null)
        {
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

    // Make CloneData public
    [System.Serializable]
    public class CloneData
    {
        public GameObject cloneObject;
        public float timer;
    }
}