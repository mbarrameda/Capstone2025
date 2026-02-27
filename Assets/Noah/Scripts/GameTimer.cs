using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 5-minute countdown timer. When time runs out, ghost wins.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Timer Settings")]
    [Tooltip("Game duration in seconds (300 = 5 minutes)")]
    public float gameDuration = 300f; // 5 minutes

    [Header("Win Scenes")]
    public string ghostWinSceneName = "GhostWinScene";
    public string explorerWinSceneName = "ExplorerWinScene"; // In case you need it

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerBackground; // Optional background for the timer

    [Header("Timer Colors")]
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    [Tooltip("Time remaining (in seconds) when color changes to warning")]
    public float warningThreshold = 60f; // 1 minute
    [Tooltip("Time remaining (in seconds) when color changes to danger")]
    public float dangerThreshold = 30f; // 30 seconds

    [Header("Audio (Optional)")]
    public AudioClip tickingSound;
    public AudioClip finalCountdownSound;
    private AudioSource audioSource;

    // Timer state
    private float timeRemaining;
    private bool timerRunning = false;
    private bool hasWon = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        timeRemaining = gameDuration;
        StartTimer();
    }

    private void Update()
    {
        if (!timerRunning || hasWon) return;

        // Count down
        timeRemaining -= Time.deltaTime;

        // Update display
        UpdateTimerDisplay();

        // Check for win condition
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            TimerExpired();
        }
        // Play warning sounds
        else if (timeRemaining <= dangerThreshold && timeRemaining > dangerThreshold - 1f)
        {
            PlaySound(finalCountdownSound);
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        // Format time as MM:SS
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Change color based on time remaining
        if (timeRemaining <= dangerThreshold)
        {
            timerText.color = dangerColor;
            // Optional: Make it pulse
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            timerText.color = Color.Lerp(dangerColor, Color.white, pulse * 0.3f);
        }
        else if (timeRemaining <= warningThreshold)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }

    private void TimerExpired()
    {
        if (hasWon) return; // Prevent multiple calls

        hasWon = true;
        timerRunning = false;

        Debug.Log("⏰ TIME'S UP! Ghost Wins!");

        // Show victory message briefly before loading win scene
        if (timerText != null)
        {
            timerText.text = "TIME'S UP!";
            timerText.color = dangerColor;
        }

        // Load ghost win scene after a short delay
        Invoke("LoadGhostWinScene", 2f);
    }

    private void LoadGhostWinScene()
    {
        if (!string.IsNullOrEmpty(ghostWinSceneName))
        {
            SceneManager.LoadScene(ghostWinSceneName);
        }
        else
        {
            Debug.LogError("Ghost win scene name not set!");
        }
    }

    public void StartTimer()
    {
        timerRunning = true;
        timeRemaining = gameDuration;
        hasWon = false;
        Debug.Log($"⏱️ Timer started: {gameDuration} seconds");
    }

    public void PauseTimer()
    {
        timerRunning = false;
    }

    public void ResumeTimer()
    {
        timerRunning = true;
    }

    public void StopTimer()
    {
        timerRunning = false;
        timeRemaining = 0;
    }

    public void AddTime(float seconds)
    {
        timeRemaining += seconds;
        Debug.Log($"⏱️ Added {seconds} seconds. New time: {timeRemaining}");
    }

    public void SubtractTime(float seconds)
    {
        timeRemaining -= seconds;
        if (timeRemaining < 0) timeRemaining = 0;
        Debug.Log($"⏱️ Subtracted {seconds} seconds. New time: {timeRemaining}");
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Call this from explorer win condition
    public void ExplorerWins()
    {
        hasWon = true;
        timerRunning = false;
        Debug.Log("🏆 Explorer Wins!");

        if (!string.IsNullOrEmpty(explorerWinSceneName))
        {
            SceneManager.LoadScene(explorerWinSceneName);
        }
    }
}