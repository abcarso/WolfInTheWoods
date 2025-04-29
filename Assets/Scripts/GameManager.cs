using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 
    public GameObject losePanel;
    public GameObject winPanel;

    private FirstPersonController player;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject); 

        player = FindFirstObjectByType<FirstPersonController>();

        // Make sure the panels are inactive
        losePanel.SetActive(false);
        winPanel.SetActive(false);
    }

    // Returns game to original state (doesn't fix apples yet)
    public void ResetGame()
    {
        if (player != null)
        {
            player.ResetPlayer();
            losePanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Player not found");
        }
    }

    // Call to show the lost screen
    public void LoseGame()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("Lose panel not found");
        }
        // Keep player from moving during the lose screen
        if (player != null)
        {
            player.DisablePlayer();
        }
    }
    // Call to show the win screen
    public void WinGame()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("Win panel not found");
        }
        // Keep player from moving during the win screen
        if (player != null)
        {
            player.DisablePlayer();
        }
    }
}
