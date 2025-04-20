using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; } // singleton instance

    public GameObject hudPanel;
    public GameObject pauseMenuScreen;
    public GameObject gameOverScreen;
    public GameObject levelCompleteScreen;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI ammoText;

    private ScoreManager scoreManager;
    private TimerController timerController;
    private PlayerEquipment playerEquipment;

    private bool isHoldingEscape = false;
    private float escapeHeldTime = 0f;
    public float escapeHoldDuration = 0.75f;

    private void Awake()
    {
        // prevent multiple instances
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
        timerController = FindFirstObjectByType<TimerController>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerEquipment = player.GetComponent<PlayerEquipment>();
        }

        //init
        SetupUIElements();

        //also reset on scene load
        ResetUIState(); 
    }

    private void Update()
    {
        // get current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // skip input handling if in MainMenu or LevelSelect
        if (currentScene == "MainMenu" || currentScene == "LevelSelect")
        {
            return;
        }
        // pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // if game is paused, start tracking escape hold time
            if (Time.timeScale == 0f)
            {
                isHoldingEscape = true;
                escapeHeldTime = Time.unscaledTime;
            }
            // otherwise toggle pause menu
            else if ((gameOverScreen == null || !gameOverScreen.activeSelf) && 
                (levelCompleteScreen == null || !levelCompleteScreen.activeSelf))
            {
                TogglePauseMenu();
            }
        }
        else if (Input.GetKeyUp(KeyCode.Escape))
        {
            // if escape was being held but not long enough to trigger main menu return,
            // and the game is paused, then unpause on key release
            if (isHoldingEscape && Time.timeScale == 0f && 
                (Time.unscaledTime - escapeHeldTime <= escapeHoldDuration))
            {
                HidePauseMenu();
            }
            
            isHoldingEscape = false;
            escapeHeldTime = 0f;
        }
        
        // check if escape is being held while paused
        if (isHoldingEscape && Time.timeScale == 0f && Time.unscaledTime - escapeHeldTime > escapeHoldDuration)
        {
            isHoldingEscape = false;
            escapeHeldTime = 0f;
            
            //hide hud and timer
            if (hudPanel != null) hudPanel.SetActive(false);
            if (timerText != null) timerText.text = "";
            
            Time.timeScale = 1f;
            
            SceneManager.LoadScene("MainMenu");
        }
    }

    //reset ui on scene load
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("reset ui");
        Time.timeScale = 1f;
        
        ResetUIState();
        SetupUIElements();
        
        if (timerText != null && timerText.gameObject != null)
        {
            timerText.gameObject.SetActive(true);
        }
    }
    
    void ResetUIState()
    {
        ShowHUD();
        HidePauseMenu();
        HideGameOver();
        HideLevelComplete();
        
        if (levelCompleteScreen != null) 
        {
            levelCompleteScreen.SetActive(false);
        }
    }

    private void SetupUIElements()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();
        timerController = FindFirstObjectByType<TimerController>();

        if (timerController != null && timerText != null)
        {
            timerController.timerText = timerText;
        }

        if (scoreManager != null && scoreText != null)
        {
            scoreManager.scoreText = scoreText;
        }

        // assign ammotext
        if (ammoText == null)
        {
            AmmoDisplay ammoDisplayComponent = FindFirstObjectByType<AmmoDisplay>();
            if (ammoDisplayComponent != null)
            {
                 ammoText = ammoDisplayComponent.ammoText;
            }
            else
            {
                Debug.LogWarning("coudl not find ammodisplay component");
            }
        }
    }

    public void ShowHUD()
    {
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    public void HideHUD()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuScreen != null)
        {
            Time.timeScale = 0f; // pause time
            pauseMenuScreen.SetActive(true);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
        }
    }

    public void HidePauseMenu()
    {
        if (pauseMenuScreen != null)
        {
            Time.timeScale = 1f; // resume time
            pauseMenuScreen.SetActive(false);
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

    public void ShowGameOver()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
            if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
            if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
            
            AmmoDisplay ammoDisplay = FindFirstObjectByType<AmmoDisplay>();
            if (ammoDisplay != null)
            {
                ammoDisplay.ResetDisplay(); //empty string instead of disabling
            }
        }
    }

    public void HideGameOver()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }

    public void ShowLevelComplete()
    {
        if (levelCompleteScreen != null)
        {
            levelCompleteScreen.SetActive(true);
            if (pauseMenuScreen != null) pauseMenuScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
            
            AmmoDisplay ammoDisplay = FindFirstObjectByType<AmmoDisplay>();
            if (ammoDisplay != null)
            {
                ammoDisplay.ResetDisplay(); // empty string instead of disabling
            }
            
            if (hudPanel != null)
            {
                hudPanel.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (scoreText != null) scoreText.gameObject.SetActive(true);
            }
        }
    }

    public void HideLevelComplete()
    {
        if (levelCompleteScreen != null)
        {
            levelCompleteScreen.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        HidePauseMenu();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; // resume time
        
        FloorAccessController.isLevelComplete = false;
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}