using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public PlayerInputHandler player;
    public GhostController ghost;

    private void Awake()
    {
        if (Gamepad.all.Count < 2)
        {
            Debug.LogError("Two controllers required!");
            return;
        }

        // Player 1
        var playerInputs = new PlayerInputs();
        playerInputs.devices = new InputDevice[] { Gamepad.all[0] };
        player.AssignInput(playerInputs);

        // Ghost
        var ghostInputs = new PlayerInputs();
        ghostInputs.devices = new InputDevice[] { Gamepad.all[1] };
        ghost.AssignInput(ghostInputs);
    }
}
