using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Buttons")]
    public Button startButton;
    public Button optionsButton;

    [Header("Options Panel")]
    public GameObject optionsPanel;
    public Button backButton;

    [Header("Sliders")]
    public Slider volumeSlider;
    public Slider brightnessSlider;

    [Header("Background")]
    public Image background;
    public Sprite backgroundA;
    public Sprite backgroundB;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip moveSound;
    public AudioClip startSound;

    [Header("Scene")]
    public string nextScene;

    private bool startingGame = false;

    void Start()
    {
        optionsPanel.SetActive(false);

        // Initialize sliders from SettingsManager
        if (SettingsManager.Instance != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
            brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 1f);
        }

        // Assign slider listeners
        volumeSlider.onValueChanged.AddListener(value =>
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetVolume(value);
        });

        brightnessSlider.onValueChanged.AddListener(value =>
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.SetBrightness(value);
        });

        // Set first selected button for controller
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    // =========================
    // MENU SOUND
    // =========================
    public void PlayMoveSound()
    {
        if (audioSource && moveSound)
            audioSource.PlayOneShot(moveSound);
    }

    // =========================
    // START GAME
    // =========================
    public void StartGame()
    {
        if (startingGame) return;

        startingGame = true;

        if (audioSource && startSound)
            audioSource.PlayOneShot(startSound);

        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        float timer = 0f;
        float duration = startSound.length;

        while (timer < duration)
        {
            background.sprite = backgroundA;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));

            background.sprite = backgroundB;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));

            timer += 0.16f;
        }

        SceneManager.LoadScene(nextScene);
    }

    // =========================
    // OPTIONS PANEL
    // =========================
    public void OpenOptions()
    {
        optionsPanel.SetActive(true);

        startButton.interactable = false;
        optionsButton.interactable = false;

        EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);

        startButton.interactable = true;
        optionsButton.interactable = true;

        EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
    }
}