using UnityEngine;

public class StartButton : MonoBehaviour
{
    public SimonSaysPuzzle puzzleManager;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Explorer") && Input.GetKeyDown(KeyCode.E))
        {
            puzzleManager.StartPuzzle();
        }
    }
}
