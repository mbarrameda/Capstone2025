using UnityEngine;
using TMPro; // Important for TextMeshPro

public class DistanceChecker : MonoBehaviour
{
    // Assign the specific UI Text components in the Inspector
   // public TextMeshProUGUI explorerDistanceDisplay;
    public TextMeshProUGUI ghostDistanceDisplay;

    // --- REVERTED TO PRIVATE: Objects are now found dynamically by tag ---
    private GameObject explorerObject;
    private GameObject ghostObject;

    void Start()
    {
        // Initial setup for the distance checker.
        // We will attempt to find the objects here, but may need to retry in LateUpdate
        // if the objects spawn AFTER this Start() runs.

        // --- STEP 1: DYNAMICALLY FIND GAME OBJECTS BY TAG ---
        FindCharacterObjects();

        // --- STEP 2: CHECK UI ASSIGNMENTS ---
      //  if (explorerDistanceDisplay == null)
       // {
        //    Debug.LogError("Distance Check FAILED: Explorer Distance Display (TextMeshPro) is not linked in the Inspector!");
       // }
        if (ghostDistanceDisplay == null)
        {
            Debug.LogError("Distance Check FAILED: Ghost Distance Display (TextMeshPro) is not linked in the Inspector!");
        }
    }

    // Helper method to look up the objects
    private void FindCharacterObjects()
    {
        // Try to find the objects by their tags
        if (explorerObject == null)
        {
            explorerObject = GameObject.FindWithTag("Explorer");
        }
        if (ghostObject == null)
        {
            ghostObject = GameObject.FindWithTag("Ghost");
        }

        if (explorerObject != null && ghostObject != null)
        {
            Debug.Log("Distance Checker successfully linked to spawned characters.");
        }
    }

    // Runs after all physics/movement logic for the frame is complete
    void LateUpdate()
    {
        // Safety check: If objects are not found (because they spawned late), try finding them again.
        if (explorerObject == null || ghostObject == null)
        {
            FindCharacterObjects();

            // If they are still null after trying to find them, disable execution and log error
            if (explorerObject == null || ghostObject == null)
            {
                // This will only happen if the objects are never spawned or tags are wrong.
                return;
            }
        }

        // Only run if we have successfully found the game objects and UI elements
        if (explorerObject != null && ghostObject != null)
        {
            // Calculate the distance based on the object's final position for this frame
            float distance = Vector3.Distance(explorerObject.transform.position, ghostObject.transform.position);
            string displayText = $"Distance: {distance:F2} meters";

            // --- REINFORCED DEBUG LINES ---
            // The Console positions SHOULD now be changing if the prefab instances are tagged correctly.
            // --- END REINFORCED DEBUG LINES ---

            // Update BOTH UI elements, checking for null just in case
           // if (explorerDistanceDisplay != null)
            //{
            //    explorerDistanceDisplay.text = displayText;
           // }

            if (ghostDistanceDisplay != null)
            {
                ghostDistanceDisplay.text = displayText;
            }
        }
    }
}