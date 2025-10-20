using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public PlayerInputHandler explorer;       // Real explorer
    public GhostController ghost;             // The ghost
    public GameObject explorerClonePrefab;    // Prefab of explorer clone

    [Header("Clone Settings")]
    public Vector3 cloneOffset = new Vector3(1.5f, 0f, 0f);
    public float fearDrainRateWhileUsingClone = 20f; // fear drained per second

    private PlayerInputs ghostInputs;
    private PlayerInputs explorerInputs;
    private GameObject currentClone;
    private bool isControllingClone = false;

    private void Awake()
    {
        if (Gamepad.all.Count < 2)
        {
            Debug.LogError("Two controllers required!");
            return;
        }

        // Setup explorer input
        explorerInputs = new PlayerInputs();
        explorerInputs.devices = new InputDevice[] { Gamepad.all[0] };
        explorer.TakeControl(explorerInputs);

        // Setup ghost input
        ghostInputs = new PlayerInputs();
        ghostInputs.devices = new InputDevice[] { Gamepad.all[1] };
        ghost.AssignInput(ghostInputs);

        // Bind clone control button
        ghostInputs.Player.Possess.performed += ctx => ToggleCloneControl();
    }

    private void Update()
    {
        if (isControllingClone)
        {
            DrainFear();

            // Auto-return to ghost if fear depleted
            if (ghost.fear <= 0f)
            {
                ReleaseClone();
            }
        }
    }

    private void ToggleCloneControl()
    {
        if (isControllingClone)
        {
            ReleaseClone();
        }
        else
        {
            if (ghost.fear <= 0f)
            {
                Debug.Log("Not enough fear to use clone!");
                return;
            }
            SpawnAndControlClone();
        }
    }

    private void SpawnAndControlClone()
    {
        if (currentClone != null) return;

        // Spawn clone at ghost position + offset
        currentClone = Instantiate(
            explorerClonePrefab,
            ghost.transform.position + cloneOffset,
            ghost.transform.rotation
        );

        // Freeze and hide ghost
        ghost.FreezeInput(true);
        ghost.SetVisibility(false);  // <-- hide ghost renderer & camera

        // Give ghost's input to clone
        var cloneHandler = currentClone.GetComponent<PlayerInputHandler>();
        if (cloneHandler != null)
        {
            ghostInputs.Disable();
            cloneHandler.TakeControl(ghostInputs);
        }

        isControllingClone = true;
    }

    private void ReleaseClone()
    {
        if (currentClone == null) return;

        // Destroy clone
        Destroy(currentClone);
        currentClone = null;

        // Unfreeze and show ghost
        ghost.FreezeInput(false);
        ghost.SetVisibility(true); // <-- restore ghost renderer & camera

        // Restore ghost input
        ghostInputs.Enable();
        ghost.AssignInput(ghostInputs);

        isControllingClone = false;
    }


    private void DrainFear()
    {
        ghost.fear -= fearDrainRateWhileUsingClone * Time.deltaTime;
        ghost.fear = Mathf.Max(ghost.fear, 0f);
    }
}
