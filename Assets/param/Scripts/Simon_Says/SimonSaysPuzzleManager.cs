using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimonSaysPuzzle : MonoBehaviour
{
    [Header("References")]
    public LightButton[] displayLights;  // Assign display wall buttons
    public LightButton[] playerLights;   // Assign player wall buttons
    public GameObject door;              // The door to unlock/disappear

    [Header("Puzzle Settings")]
    public int patternLength = 4;        // How long the random sequence is
    public float lightOnTime = 0.6f;     // Duration each light stays lit
    public float betweenLights = 0.3f;   // Pause between lights
    public Color displayColor = Color.cyan;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    private List<int> pattern = new List<int>();
    private int playerProgress = 0;
    private bool puzzleActive = false;
    private bool showingPattern = false;

    void Start()
    {
        TurnOffAllLights();
    }

    // Called by Start Button
    public void StartPuzzle()
    {
        if (showingPattern) return;

        TurnOffAllLights();
        GeneratePattern();
        playerProgress = 0;
        puzzleActive = false;

        StartCoroutine(ShowPattern());
    }

    // Generate a new random pattern
    void GeneratePattern()
    {
        pattern.Clear();
        for (int i = 0; i < patternLength; i++)
        {
            int randomIndex = Random.Range(0, displayLights.Length);
            pattern.Add(randomIndex);
        }
    }

    // Light up the pattern on display wall
    IEnumerator ShowPattern()
    {
        showingPattern = true;
        yield return new WaitForSeconds(0.5f);

        foreach (int index in pattern)
        {
            displayLights[index].TurnOn(displayColor);
            yield return new WaitForSeconds(lightOnTime);
            displayLights[index].TurnOff();
            yield return new WaitForSeconds(betweenLights);
        }

        showingPattern = false;
        puzzleActive = true;
    }

    // Called by Player Lights when pressed
    public void OnPlayerPressed(int pressedIndex)
    {
        if (!puzzleActive || showingPattern) return;

        // Check if correct light was pressed
        if (pressedIndex == pattern[playerProgress])
        {
            playerProgress++;

            // If finished pattern successfully
            if (playerProgress >= pattern.Count)
            {
                puzzleActive = false;
                StartCoroutine(PuzzleSuccess());
            }
        }
        else
        {
            // Wrong input
            puzzleActive = false;
            StartCoroutine(PuzzleFail());
        }
    }

    // When puzzle is solved correctly
    IEnumerator PuzzleSuccess()
    {
        Debug.Log("Puzzle Solved!");
        TurnOffAllLights();

        // Flash success color on all lights
        foreach (var l in displayLights) l.TurnOn(successColor, 5f);
        foreach (var l in playerLights) l.TurnOn(successColor, 5f);
        yield return new WaitForSeconds(1f);
        TurnOffAllLights();

        // Open door (make it disappear)
        if (door != null)
            door.SetActive(false);
    }

    // When player fails
    IEnumerator PuzzleFail()
    {
        Debug.Log("Puzzle Failed!");
        TurnOffAllLights();

        // Flash red on all lights
        foreach (var l in displayLights) l.TurnOn(failColor, 5f);
        foreach (var l in playerLights) l.TurnOn(failColor, 5f);
        yield return new WaitForSeconds(1f);
        TurnOffAllLights();

        // Optionally restart pattern
        yield return new WaitForSeconds(0.5f);
        StartPuzzle();
    }

    void TurnOffAllLights()
    {
        foreach (var l in displayLights) l.TurnOff();
        foreach (var l in playerLights) l.TurnOff();
    }
}
