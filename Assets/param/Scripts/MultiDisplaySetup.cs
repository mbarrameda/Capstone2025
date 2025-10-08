using UnityEngine;

public class MultiDisplaySetup : MonoBehaviour
{
    void Start()
    {
        // Activate all connected displays
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
    }
}
