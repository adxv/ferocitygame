using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // ADDED: For scene management

public class UIManager : MonoBehaviour
{
    // Singleton pattern
    public static UIManager Instance { get; private set; }

    [Header("UI Panels/Screens")]
    public GameObject hudPanel;
    public GameObject pauseMenuScreen;
    public GameObject gameOverScreen;
    public GameObject levelCompleteScreen;

    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI ammoText;

    // References to other managers
    private ScoreManager scoreManager;
    private TimerController timerController;
    private PlayerEquipment playerEquipment;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // ADDED: Subscribe to scene loaded event
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ADDED: Unsubscribe from scene loaded event
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Find references to other managers
        scoreManager = FindObjectOfType<ScoreManager>();
        timerController = FindObjectOfType<TimerController>();

        // Find player equipment
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerEquipment = player.GetComponent<PlayerEquipment>();
             if (playerEquipment != null)
             {
                 // Subscribe for potential future use if needed, but weapon display is now in AmmoDisplay
                 // playerEquipment.OnWeaponChanged += UpdateWeaponDisplay; // Commented out
             }
        }

        // Initial setup
        SetupUIElements();

        // Initialize UI state - This will also be called by OnSceneLoaded
        ResetUIState(); 
    }

    private void Update()
    {
        // Check for pause input (escape key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ensure we don't pause if game over or level complete screen is showing
            if ((gameOverScreen == null || !gameOverScreen.activeSelf) && 
                (levelCompleteScreen == null || !levelCompleteScreen.activeSelf))
            {
                 TogglePauseMenu();
            }
        }
    }

    private void OnDestroy()
    {
         // Unsubscribe from events if subscribed
         // if (playerEquipment != null)
         // {
         //     playerEquipment.OnWeaponChanged -= UpdateWeaponDisplay;
         // }
    }

    // ADDED: Reset UI state when a scene loads
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("UIManager: Scene Loaded, resetting UI state.");
        ResetUIState();
        // Also re-find managers potentially destroyed/recreated in the new scene
        SetupUIElements(); 
    }
    
    // ADDED: Helper method to reset UI state
    void ResetUIState()
    {
        Time.timeScale = 1f; // Ensure game is not paused
        ShowHUD();
        HidePauseMenu();
        HideGameOver();
        HideLevelComplete();
    }

    // Setup UI elements and connections
    private void SetupUIElements()
    {
        // Connect UI elements to managers (Managers will update their own text)
        // Find potentially new instances after scene load
        scoreManager = FindObjectOfType<ScoreManager>();
        timerController = FindObjectOfType<TimerController>();

        if (timerController != null && timerText != null)
        {
            timerController.timerText = timerText;
        }

        if (scoreManager != null && scoreText != null)
        {
            scoreManager.scoreText = scoreText;
        }

        // Find and assign AmmoDisplay's text if not assigned in inspector
        if (ammoText == null)
        {
            AmmoDisplay ammoDisplayComponent = FindObjectOfType<AmmoDisplay>();
            if (ammoDisplayComponent != null)
            {
                 ammoText = ammoDisplayComponent.ammoText;
                 // Also link AmmoDisplay's icon if needed by other systems, though it handles itself
                 // weaponIconImage = ammoDisplayComponent.weaponIconImage;
            }
            else
            {
                // If AmmoDisplay is part of HUD, it might not exist if HUD is initially inactive
                // Optionally log a warning or handle differently
                 Debug.LogWarning("UIManager: Could not find AmmoDisplay component to assign ammoText.");
            }
        }
        // Removed direct ammo/weapon update from UIManager start - handled by AmmoDisplay
    }

    // ------ UI State Management ------

    public void ShowHUD()
    {
        if (hudPanel != null) hudPanel.SetActive(true);
        // Hide menus when HUD shows
        // REMOVED redundant calls from here, handled by ResetUIState or specific show methods
        // HidePauseMenu(); 
        // HideGameOver();
        // HideLevelComplete();
    }

    public void HideHUD()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    // --- Pause Menu --- //
    public void ShowPauseMenu()
    {
        if (pauseMenuScreen != null)
        {
            Time.timeScale = 0f; // Pause the game
            pauseMenuScreen.SetActive(true);
            // Ensure other menus are hidden if they share a parent or overlap
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
            // Optional: Hide HUD when paused?
            // HideHUD();
        }
    }

    public void HidePauseMenu()
    {
        if (pauseMenuScreen != null)
        {
            Time.timeScale = 1f; // Resume the game
            pauseMenuScreen.SetActive(false);
            // Optional: Show HUD when unpausing if it was hidden
            // ShowHUD();
        }
    }

    public void TogglePauseMenu()
    {
        if (pauseMenuScreen != null && pauseMenuScreen.activeSelf)
        {
            HidePauseMenu();
        }
        else
        {
            ShowPauseMenu();
        }
    }

    // --- Game Over Screen --- //
    public void ShowGameOver()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
             // Ensure other menus are hidden
            if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
             HideHUD(); // Typically hide HUD on game over
        }
    }

    public void HideGameOver()
    {
        if (gameOverScreen != null)
        {
             // Only resume time if restarting, handle this in button calls
             // Time.timeScale = 1f; 
            gameOverScreen.SetActive(false);
        }
    }

    // --- Level Complete Screen --- //
    public void ShowLevelComplete()
    {
        if (levelCompleteScreen != null)
        {
            levelCompleteScreen.SetActive(true);
             // Ensure other menus are hidden
            if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
             HideHUD(); // Typically hide HUD on level complete
             Time.timeScale = 0f; // Often good to pause on level complete
        }
    }

    public void HideLevelComplete()
    {
        if (levelCompleteScreen != null)
        {
             // Only resume time if going to next level/restarting, handle in button calls
             // Time.timeScale = 1f;
            levelCompleteScreen.SetActive(false);
        }
    }

    // ------ Display Updates ------

    // Removed UpdateWeaponDisplay - AmmoDisplay.cs handles weapon icon and ammo text updates.

    // ------ Button Actions (Add these methods to call from buttons) ------

    public void ResumeGame()
    {
        HidePauseMenu();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // Ensure time is resumed before loading
        // Use UnityEngine.SceneManagement
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game..."); // Log for editor testing
        Application.Quit();
    }

    // Add method for "Next Level" if applicable
    // public void LoadNextLevel()
    // {
    //     Time.timeScale = 1f;
    //     // Load your next scene, e.g., SceneManager.LoadScene("Level2");
    // }

}