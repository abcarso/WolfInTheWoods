using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 
    public GameObject startMenu;
    public GameObject losePanel;
    public GameObject winPanel;
    public GameObject ambientaAudio;

    private bool isGameStarted = false;

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
        Debug.Log($"[GM] Player in Awake: {(player ? player.name : "null")}");

        // Make sure the panels are inactive
        losePanel.SetActive(false);
        winPanel.SetActive(false);
        // Keep audio off until game starts
        ambientaAudio.SetActive(false);
    }

    private void Start()
    {
        if (player != null)
        {
            player.DisablePlayer();
        }
    }

    private void Update()
    {
        if (!isGameStarted)
        {
            // Freeze time while on Start Menu
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
    
    // Unpause Game
    public void StartGame()
    {
        if (startMenu != null)
        {
            startMenu.SetActive(false);
        }

        isGameStarted = true;

        if (player != null)
        {
            player.EnablePlayer();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ambientaAudio.SetActive(true);
    }


    // Returns game to original state (doesn't fix apples yet)
    public void ResetGame()
    {
        if (player != null)
        {
            losePanel.SetActive(false);
            winPanel.SetActive(false);
            player.ResetPlayer();
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
