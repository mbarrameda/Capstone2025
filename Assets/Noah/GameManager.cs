using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public PlayerInputHandler explorer;
    public GhostController ghost;

    [Header("Possession Settings")]
    public float minFearToPossess = 50f;
    public float possessionDrainRate = 10f; // fear per second

    private PlayerInputs explorerInputs;
    private PlayerInputs ghostInputs;

    private bool isPossessing = false;

    private void Awake()
    {
        // Make sure we have two controllers
        if (Gamepad.all.Count < 2)
        {
            Debug.LogError("Two gamepads are required (Explorer & Ghost)");
            return;
        }

        // --- Player 1 (Explorer) ---
        explorerInputs = new PlayerInputs();
        explorerInputs.devices = new InputDevice[] { Gamepad.all[0] };
        explorer.TakeControl(explorerInputs);

        // --- Player 2 (Ghost) ---
        ghostInputs = new PlayerInputs();
        ghostInputs.devices = new InputDevice[] { Gamepad.all[1] };
        ghost.AssignInput(ghostInputs);

        // Bind ghost possession control (Y / north button)
        ghostInputs.Player.Possess.performed += ctx => TryTogglePossession();
    }

    private void Update()
    {
        if (isPossessing)
        {
            // Drain ghost’s fear while possessing
            ghost.fear -= possessionDrainRate * Time.deltaTime;
            if (ghost.fear <= 0f)
            {
                ghost.fear = 0f;
                EndPossession();
            }
        }
    }

    private void TryTogglePossession()
    {
        if (isPossessing)
        {
            EndPossession();
        }
        else if (ghost.fear >= minFearToPossess)
        {
            StartPossession();
        }
        else
        {
            Debug.Log("Not enough fear to possess!");
        }
    }

    private void StartPossession()
    {
        isPossessing = true;

        // Disable explorer’s own input
        explorerInputs.Disable();

        // Let ghost control explorer instead
        ghostInputs.Disable(); // temporarily disable ghost movement
        explorer.TakeControl(ghostInputs);

        Debug.Log("👻 Ghost is now possessing the Explorer!");
    }

    private void EndPossession()
    {
        isPossessing = false;

        // Return controls to normal
        explorerInputs.Enable();
        explorer.TakeControl(explorerInputs);

        ghostInputs.Enable();
        ghost.AssignInput(ghostInputs);

        Debug.Log("💨 Ghost has left the body!");
    }
}
