using UnityEngine;
using UnityEngine.UI;

public class BatteryManager : MonoBehaviour
{
    public static BatteryManager Instance;

    private Slider batteryBar;
    private bool phoneIsOut = false;
    private float currentBatteryValue = 100f;

    void Awake()
    {
        // Make this a singleton so it's easily accessible
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create battery UI if it doesn't exist
        CreateBatteryUI();
    }

    void CreateBatteryUI()
    {
        // Check if battery bar already exists
        GameObject existingBar = GameObject.Find("BatteryBar");
        if (existingBar != null)
        {
            batteryBar = existingBar.GetComponent<Slider>();
        }
        else
        {
            // Create new battery bar
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("Canvas");
                canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Create slider
            GameObject sliderObject = new GameObject("BatteryBar");
            sliderObject.transform.SetParent(canvasObject.transform);
            batteryBar = sliderObject.AddComponent<Slider>();

            // Set up rect transform
            RectTransform rt = sliderObject.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.9f);  // Left side
            rt.anchorMax = new Vector2(0.15f, 0.9f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200, 20);
            rt.anchoredPosition = Vector2.zero;

            // Set up slider values
            batteryBar.minValue = 0;
            batteryBar.maxValue = 100;
            batteryBar.value = 100;

            // Create background
            GameObject bgObject = new GameObject("Background");
            bgObject.transform.SetParent(sliderObject.transform);
            Image bgImage = bgObject.AddComponent<Image>();
            bgImage.color = Color.gray;
            RectTransform bgRT = bgObject.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Create fill area and fill
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0);
            fillAreaRT.anchorMax = new Vector2(1, 1);
            fillAreaRT.sizeDelta = new Vector2(-20, 0);

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillArea.transform);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = Color.green;
            RectTransform fillRT = fillObject.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0, 0);
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.sizeDelta = new Vector2(10, 0);

            batteryBar.fillRect = fillRT;
        }

        // Start hidden
        batteryBar.gameObject.SetActive(false);
    }

    void Update()
    {
        // Always keep the battery bar in the correct state
        if (batteryBar != null)
        {
            batteryBar.gameObject.SetActive(phoneIsOut);
            batteryBar.value = currentBatteryValue;
        }
    }

    // Public methods to control the battery UI
    public void SetPhoneState(bool isOut)
    {
        phoneIsOut = isOut;
    }

    public void SetBatteryValue(float value)
    {
        currentBatteryValue = Mathf.Clamp(value, 0, 100);
    }
}