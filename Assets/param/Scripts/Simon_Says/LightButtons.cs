using UnityEngine;

public class LightButton : MonoBehaviour
{
    [Header("Settings")]
    public int lightIndex; // Unique ID for this light (0, 1, 2...)
    public SimonSaysPuzzle puzzleManager; // Reference to the main puzzle script
    public Light lightSource; // Assign the child Point Light here

    private bool isOn = false;

    // Turn on the Unity Light
    public void TurnOn(Color color, float intensity = 3f)
    {
        if (lightSource == null) return;
        lightSource.color = color;
        lightSource.intensity = intensity;
        isOn = true;
    }

    // Turn off the Unity Light
    public void TurnOff()
    {
        if (lightSource == null) return;
        lightSource.intensity = 0f;
        isOn = false;
    }

    // Called when the player presses this button
    public void PlayerPress()
    {
        if (puzzleManager != null)
        {
            puzzleManager.OnPlayerPressed(lightIndex);
        }

        // Flash light as feedback
        StartCoroutine(FlashLight());
    }

    // Briefly flash light for player feedback
    private System.Collections.IEnumerator FlashLight()
    {
        TurnOn(Color.yellow, 4f);
        yield return new WaitForSeconds(0.25f);
        TurnOff();
    }
}
