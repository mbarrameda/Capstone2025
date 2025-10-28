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
    [Tooltip("Offset from ghost when spawning clone")]
    public Vector3 cloneOffset = new Vector3(1.5f, 0f, 0f);

    [Tooltip("Fear drained per second while controlling clone")]
    public float fearDrainRateWhileUsingClone = 20f;

    [Tooltip("Maximum duration for controlling a clone in seconds")]
    public float cloneDuration = 15f;

    [Tooltip("Fear cost to spawn a clone")]
    public float cloneFearCost = 25f;

    private List<PlayerInputHandler> explorers = new List<PlayerInputHandler>();
    private List<GhostController> ghosts = new List<GhostController>();
    private List<PlayerInputs> inputSets = new List<PlayerInputs>();

    // Tracks active clones
    private Dictionary<GhostController, CloneData> activeClones = new Dictionary<GhostController, CloneData>();

    private void Start()
    {
        SetupPlayers();
        SetupDisplays();
    }

    private void Update()
    {
        // Update all active clones
        List<GhostController> finishedClones = new List<GhostController>();

        foreach (var kvp in activeClones)
        {
            GhostController ghost = kvp.Key;
            CloneData data = kvp.Value;

            // Drain fear and count down timer
            ghost.fear -= fearDrainRateWhileUsingClone * Time.deltaTime;
            ghost.fear = Mathf.Max(ghost.fear, 0f);
            data.timer -= Time.deltaTime;

            // Auto-release if time runs out or fear depleted
            if (data.timer <= 0f || ghost.fear <= 0f)
            {
                finishedClones.Add(ghost);
            }
        }

        foreach (var ghost in finishedClones)
        {
            ReleaseClone(ghost);
        }
    }

    private void SetupPlayers()
    {
        int controllerCount = Gamepad.all.Count;
        Debug.Log($"Detected {controllerCount} controllers.");

        if (controllerCount < 2)
        {
            Debug.LogError("At least 2 controllers required!");
            return;
        }

        // Create an input set for each controller
        for (int i = 0; i < controllerCount; i++)
        {
            var input = new PlayerInputs();
            input.devices = new InputDevice[] { Gamepad.all[i] };
            inputSets.Add(input);
        }

        // --- PLAYER ASSIGNMENT LOGIC ---
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

        // Bind clone/possess button per ghost
        int index = ghosts.Count - 1;
        inputSets[controllerIndex].Player.Possess.performed += ctx => ToggleCloneControl(ghost);
    }

    private void SetupDisplays()
    {
        if (Display.displays.Length > 1)
        {
            Display.displays[0].Activate();
            Display.displays[1].Activate();
        }

        // --- Explorers ---
        for (int i = 0; i < explorers.Count; i++)
        {
            Camera cam = explorers[i].playerCamera;
            cam.targetDisplay = 0;

            if (explorers.Count == 1)
                cam.rect = new Rect(0, 0, 1, 1);
            else if (explorers.Count == 2)
                cam.rect = i == 0 ? new Rect(0, 0.5f, 1, 0.5f) : new Rect(0, 0, 1, 0.5f);
        }

        // --- Ghosts ---
        for (int i = 0; i < ghosts.Count; i++)
        {
            Camera cam = ghosts[i].ghostCamera;
            cam.targetDisplay = 1;

            if (ghosts.Count == 1)
                cam.rect = new Rect(0, 0, 1, 1);
            else if (ghosts.Count == 2)
                cam.rect = i == 0 ? new Rect(0, 0.5f, 1, 0.5f) : new Rect(0, 0, 1, 0.5f);
        }
    }

    // --- Clone / Possess Logic ---
    private void ToggleCloneControl(GhostController ghost)
    {
        if (activeClones.ContainsKey(ghost))
        {
            ReleaseClone(ghost);
        }
        else
        {
            if (ghost.fear < cloneFearCost) return;
            SpawnAndControlClone(ghost);
        }
    }

    private void SpawnAndControlClone(GhostController ghost)
    {
        if (activeClones.ContainsKey(ghost)) return;

        // Deduct fear cost
        ghost.fear -= cloneFearCost;

        GameObject clone = Instantiate(explorerClonePrefab, ghost.transform.position + cloneOffset, ghost.transform.rotation);

        ghost.FreezeInput(true);
        ghost.SetVisibility(false);

        var cloneHandler = clone.GetComponent<PlayerInputHandler>();
        if (cloneHandler != null)
        {
            ghost.inputActions.Disable();
            cloneHandler.TakeControl(ghost.inputActions);
        }

        activeClones.Add(ghost, new CloneData { cloneObject = clone, timer = cloneDuration });
    }

    private void ReleaseClone(GhostController ghost)
    {
        if (!activeClones.ContainsKey(ghost)) return;

        var data = activeClones[ghost];

        if (data.cloneObject != null)
            Destroy(data.cloneObject);

        ghost.FreezeInput(false);
        ghost.SetVisibility(true);
        ghost.inputActions.Enable();
        ghost.AssignInput(ghost.inputActions);

        activeClones.Remove(ghost);
    }

    private class CloneData
    {
        public GameObject cloneObject;
        public float timer;
    }
}
