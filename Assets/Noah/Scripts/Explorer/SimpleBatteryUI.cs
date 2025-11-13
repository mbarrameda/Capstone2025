using UnityEngine;
using UnityEngine.UI;

public class SimpleBatteryUI : MonoBehaviour
{
    private Slider batteryBar;

    void Start()
    {
        // Find battery UI by name
        GameObject batteryObj = GameObject.Find("BatteryBar");
        if (batteryObj != null)
        {
            batteryBar = batteryObj.GetComponent<Slider>();
            batteryBar.gameObject.SetActive(false); // Start hidden
        }
    }

    void Update()
    {
        if (batteryBar == null) return;

        // Directly check if this phone object is active
        bool phoneIsOut = gameObject.activeSelf;
        batteryBar.gameObject.SetActive(phoneIsOut);
    }
}
