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

    [Header("Combo System")]
    public float comboTimeWindow = 3f; // Time window in seconds before combo expires
    public float comboMultiplierBase = 1.5f; // How much each combo tier multiplies score

    // Score components
    private int baseKillScore = 100; // Base score per kill
    private int shotsFired = 0;
    private int shotsHit = 0;
    private int enemiesTotal = 0;
    private int enemiesDefeated = 0;
    private float startTime = 0f;
    private float endTime = 0f;
    private bool levelActive = false;
    private int currentScore = 0;
    private int killsScore = 0;
    private int comboBonus = 0;
    private int timeBonus = 0;
    
    // Combo system
    private int currentCombo = 0;
    private float lastKillTime = 0f;
    private Coroutine comboTimerCoroutine;

    // Grade system ratings
    private string currentGrade = "D";

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
        killsScore = 0;
        comboBonus = 0;
        timeBonus = 0;
        currentCombo = 0;
        lastKillTime = 0f;
        currentGrade = "D";
        
        // Reset UI
        if (scoreText != null)
        {
            scoreText.text = "0";
        }
        
        // Stop combo timer if it's running
        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
            comboTimerCoroutine = null;
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
        killsScore = 0;
        comboBonus = 0;
        timeBonus = 0;
        currentCombo = 0;
        lastKillTime = 0f;

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
        
        // Increase combo
        currentCombo++;
        lastKillTime = Time.time;
        
        // Calculate kill score with combo multiplier
        float comboMultiplier = 1f + (currentCombo - 1) * 0.1f; // Each combo adds 10% more
        int scoreForKill = Mathf.RoundToInt(baseKillScore * comboMultiplier);
        
        // Add to score components
        killsScore += scoreForKill;
        comboBonus += scoreForKill - baseKillScore; // The extra from combo is tracked separately
        
        // Update total score
        currentScore = killsScore;
        
        // Update UI
        UpdateScoreUI();
        
        // Handle combo timer
        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
        }
        comboTimerCoroutine = StartCoroutine(ComboTimer());
        
        // Increment defeated count
        enemiesDefeated++;
        
        // Check for level completion
        if (enemiesDefeated >= enemiesTotal)
        {
            EndLevel();
        }
    }
    
    private IEnumerator ComboTimer()
    {
        yield return new WaitForSeconds(comboTimeWindow);
        
        // Reset combo if time ran out
        if (Time.time - lastKillTime >= comboTimeWindow)
        {
            Debug.Log($"Combo of {currentCombo} expired");
            currentCombo = 0;
        }
    }

    void EndLevel()
    {
        endTime = Time.time;
        levelActive = false;
        CalculateFinalScore();
        Debug.Log("Level Ended. Calculating final score.");

        // Find any object that has a ShowLevelComplete method
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
        
        // Store the data for the score screen
        ScoreScreenManager.KillsScore = killsScore;
        ScoreScreenManager.ComboBonus = comboBonus;
        ScoreScreenManager.TimeBonus = timeBonus;
        ScoreScreenManager.Accuracy = GetAccuracy();
        ScoreScreenManager.FinalScore = currentScore;
        ScoreScreenManager.Grade = currentGrade;
        ScoreScreenManager.CompletionTime = endTime - startTime;
    }

    void CalculateFinalScore()
    {
        float elapsedTime = endTime - startTime;
        float accuracy = GetAccuracy();

        // Calculate time bonus (higher for faster completion)
        // Dynamic target time based on enemy count (6 seconds per enemy)
        float targetTime = Mathf.Max(90f, enemiesTotal * 6f); // Minimum 90 seconds, or 6 seconds per enemy
        float timeSaved = Mathf.Max(0, targetTime - elapsedTime);
        timeBonus = Mathf.RoundToInt(timeSaved * 5f); // Reduced multiplier for more balanced scoring

        // Calculate final score with accuracy multiplier
        float accuracyMultiplier = Mathf.Max(0.1f, accuracy); // At least 10% even with 0 accuracy
        int scoreWithAccuracy = Mathf.RoundToInt((killsScore + comboBonus) * accuracyMultiplier);
        
        // Total score = score with accuracy + time bonus
        currentScore = scoreWithAccuracy + timeBonus;
        
        // Calculate grade (mostly based on accuracy)
        CalculateGrade(accuracy, elapsedTime);

        Debug.Log($"Final Score: Kills ({killsScore}) + Combo Bonus ({comboBonus}) * Accuracy ({accuracy:P2}) + Time Bonus ({timeBonus}) = {currentScore}");
        Debug.Log($"Target time: {targetTime}s for {enemiesTotal} enemies");
    }
    
    private void CalculateGrade(float accuracy, float completionTime)
    {
        // Accuracy has higher weight (80%) in grade calculation
        // Time has lower weight (20%)
        
        if (accuracy >= 1.0f) 
        {
            currentGrade = "SS"; // Perfect accuracy
        }
        else if (accuracy >= 0.85f)
        {
            currentGrade = "S";  // 85%+ accuracy
        }
        else if (accuracy >= 0.65f)
        {
            currentGrade = "A";  // 65%+ accuracy
        }
        else if (accuracy >= 0.50f)
        {
            currentGrade = "B";  // 50%+ accuracy
        }
        else if (accuracy >= 0.30f)
        {
            currentGrade = "C";  // 30%+ accuracy
        }
        else
        {
            currentGrade = "D";  // Below 30% accuracy
        }
        
        // Dynamic target time based on enemy count
        float targetTime = Mathf.Max(90f, enemiesTotal * 6f); // Minimum 90 seconds, or 6 seconds per enemy
        float exceptionalTime = targetTime * 0.6f; // 60% of target time is exceptional
        float penaltyTime = targetTime * 1.5f; // 150% of target time triggers grade penalty
        
        // Time can bump grade up or down by one level if exceptionally good/bad
        if (completionTime < exceptionalTime && currentGrade != "SS") // Exceptional time
        {
            // Exceptional time can bump grade up (unless already SS)
            string[] grades = { "D", "C", "B", "A", "S", "SS" };
            int currentIndex = System.Array.IndexOf(grades, currentGrade);
            if (currentIndex < grades.Length - 1)
            {
                currentGrade = grades[currentIndex + 1];
            }
        }
        else if (completionTime > penaltyTime && currentGrade != "D") // Too slow
        {
            // Very slow time can bump grade down (unless already D)
            string[] grades = { "D", "C", "B", "A", "S", "SS" };
            int currentIndex = System.Array.IndexOf(grades, currentGrade);
            if (currentIndex > 0)
            {
                currentGrade = grades[currentIndex - 1];
            }
        }
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
    
    public int GetKillsScore()
    {
        return killsScore;
    }
    
    public int GetComboBonus()
    {
        return comboBonus;
    }

    public float GetAccuracy()
    {
        return (shotsFired > 0) ? (float)shotsHit / shotsFired : 1.0f;
    }

    public int GetEnemiesDefeated()
    {
        return enemiesDefeated;
    }

    public int GetTotalEnemies()
    {
        return enemiesTotal;
    }

    public int GetTimeBonus()
    {
        return timeBonus;
    }
    
    public string GetGrade()
    {
        return currentGrade;
    }

    public float GetElapsedTime()
    {
        return endTime - startTime;
    }

    // Optional: Call this if enemies can spawn mid-level
    public void RegisterEnemy()
    {
        enemiesTotal++;
    }
}
