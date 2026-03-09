using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Brightness Overlay")]
    public Image blackOverlay; // Full-screen black UI Image
    [Range(0f, 1f)]
    public float brightnessSmoothSpeed = 5f;

    private float targetBrightness = 1f; // 1 = fully bright
    private float targetVolume = 1f;     // 1 = max volume

    void Awake()
    {
        // Singleton pattern
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

        // Always start at full brightness and max volume
        targetBrightness = 1f;
        targetVolume = 1f;

        // Force overlay fully transparent at start
        if (blackOverlay)
        {
            Color c = blackOverlay.color;
            c.a = 0f;
            blackOverlay.color = c;
        }

        // Force max volume
        AudioListener.volume = targetVolume;

        // Save the defaults in PlayerPrefs so sliders show max on first launch
        PlayerPrefs.SetFloat("Volume", targetVolume);
        PlayerPrefs.SetFloat("Brightness", targetBrightness);
        PlayerPrefs.Save();
    }

    void Update()
    {
        // Smoothly update overlay alpha for brightness changes
        if (blackOverlay)
        {
            Color c = blackOverlay.color;
            float targetAlpha = 1f - targetBrightness;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * brightnessSmoothSpeed);
            blackOverlay.color = c;
        }
    }

    // =========================
    // Public functions for sliders
    // =========================
    public void SetVolume(float value)
    {
        targetVolume = value;
        AudioListener.volume = targetVolume;

        PlayerPrefs.SetFloat("Volume", targetVolume);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value)
    {
        targetBrightness = value;

        PlayerPrefs.SetFloat("Brightness", targetBrightness);
        PlayerPrefs.Save();
    }
}