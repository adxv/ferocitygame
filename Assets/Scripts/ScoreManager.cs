using UnityEngine;
using TMPro; // Keep: Needed for TextMeshPro
using System.Collections.Generic; // Keep: Needed for potential future use
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Reflection; // Add for reflection support
using static FloorAccessController;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public TextMeshProUGUI scoreText; // Keep: Inspector assignment info

    private int shotsFired = 0;
    private int shotsHit = 0;
    private int enemiesTotal = 0;
    private int enemiesDefeated = 0;
    private float startTime = 0f;
    private float endTime = 0f;
    private bool levelActive = false;
    private int currentScore = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void OnEnable()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset state on scene reload
        ResetState();
        
        // Start level automatically after a small delay
        Invoke("StartLevel", 0.1f);
    }

    void ResetState()
    {
        shotsFired = 0;
        shotsHit = 0;
        enemiesDefeated = 0;
        enemiesTotal = 0;
        startTime = 0f;
        endTime = 0f;
        levelActive = false;
        currentScore = 0;
        
        // Reset UI
        if (scoreText != null)
        {
            scoreText.text = "0";
        }
    }

    void Start()
    {
        // Initial setup
        if (scoreText != null)
        {
            scoreText.text = "0"; // Initialize with 0 rather than empty string
        }
        else
        {
            Debug.LogWarning("ScoreManager: Score Text UI not assigned!");
        }
        
        // Start level automatically after a small delay
        Invoke("StartLevel", 0.1f);
    }

    void Update()
    {
        // Score text update is now handled immediately when score changes
        // If you want continuous updates, uncomment below:
        // if (scoreText != null)
        // {
        //     scoreText.text = currentScore.ToString();
        // }
    }

    public void StartLevel()
    {
        shotsFired = 0;
        shotsHit = 0;
        enemiesDefeated = 0;
        startTime = Time.time;
        levelActive = true;
        currentScore = 0;

        var allEnemies = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(obj => obj.GetType().Name == "Enemy")
            .ToArray();
        enemiesTotal = allEnemies.Length;

        Debug.Log($"Level started. Enemies found: {enemiesTotal}");

        if (scoreText != null)
        {
            scoreText.text = ""; // Clear score text at level start
        }
    }

    public void RecordShotFired()
    {
        if (!levelActive) return;
        shotsFired++;
    }

    public void RecordHit()
    {
        if (!levelActive) return;
        shotsHit++;
    }

    public void RecordEnemyDefeated()
    {
        if (!levelActive) return;
        enemiesDefeated++;
        currentScore += 250; // Add points directly
        UpdateScoreUI(); // Update UI immediately

        if (enemiesDefeated >= enemiesTotal)
        {
            EndLevel();
        }
    }

    void EndLevel()
    {
        endTime = Time.time;
        levelActive = false;
        CalculateFinalScore();
        Debug.Log("Level Ended. Calculating final score.");

        // Find any object that has a ShowLevelComplete method
        var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        bool levelCompleteShown = false;
           // In your game completion logic:
        FloorAccessController.isLevelComplete = true;
        
        foreach (var behaviour in allMonoBehaviours)
        {
            // Try to find the UIManager by checking for a levelCompleteScreen field
            var fields = behaviour.GetType().GetFields(System.Reflection.BindingFlags.Public | 
                                                      System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.Name == "levelCompleteScreen")
                {
                    // Found the UIManager, call ShowLevelComplete
                    behaviour.SendMessage("ShowLevelComplete", null, SendMessageOptions.DontRequireReceiver);
                    Debug.Log($"Sent ShowLevelComplete to {behaviour.name}");
                    levelCompleteShown = true;
                    break;
                }
            }
            
            if (levelCompleteShown)
                break;
        }
        
        if (!levelCompleteShown)
        {
            Debug.LogWarning("Could not find an object with levelCompleteScreen field to show level complete screen");
        }
    }

    void CalculateFinalScore()
    {
        float elapsedTime = endTime - startTime;
        float accuracy = (shotsFired > 0) ? (float)shotsHit / shotsFired : 1.0f;

        int enemyKillScore = currentScore;
        float accuracyMultiplier = accuracy;
        float timeSaved = Mathf.Max(0, 60f - elapsedTime);
        int timeBonus = Mathf.RoundToInt(timeSaved * 10f * 15f);

        int finalScore = Mathf.RoundToInt(enemyKillScore * accuracyMultiplier) + timeBonus;

        // Update score directly, no animation
        currentScore = finalScore;
        UpdateScoreUI();

        Debug.Log($"score: kills: {enemyKillScore} * accuracy ({accuracy:P2}) + time bonus ({timeSaved:F1}s saved): {timeBonus} = Total: {finalScore}");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    // Optional: Call this if enemies can spawn mid-level
    public void RegisterEnemy()
    {
        enemiesTotal++;
        // No UpdateUI here
    }
}
