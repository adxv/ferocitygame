using UnityEngine;
using TMPro; // Keep: Needed for TextMeshPro
using System.Collections.Generic; // Keep: Needed for potential future use

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

    void Start()
    {
        if (scoreText != null)
        {
            scoreText.text = ""; // Clear score text at start
        }
        else
        {
            Debug.LogWarning("ScoreManager: Score Text UI not assigned!");
        }
    }

    public void StartLevel()
    {
        shotsFired = 0;
        shotsHit = 0;
        enemiesDefeated = 0;
        startTime = Time.time;
        levelActive = true;
        currentScore = 0; // Keep: Explains score reset

        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
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
        currentScore += 250; // Add points for defeating (updated value)

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
    }

    void CalculateFinalScore()
    {
        float elapsedTime = endTime - startTime;
        float accuracy = (shotsFired > 0) ? (float)shotsHit / shotsFired : 1.0f; // Avoid division by zero, default to 1.0 if no shots fired

        // --- Final Score Calculation --- 
        // Base score from enemy kills
        int enemyKillScore = currentScore; // Score before bonuses

        // Accuracy Multiplier (0.0 to 1.0)
        float accuracyMultiplier = accuracy;

        // Time Bonus (Heavily weighted)
        // Target time: 60 seconds
        // Bonus: 15 points per 0.1 seconds saved under 60s
        float timeSaved = Mathf.Max(0, 60f - elapsedTime);
        int timeBonus = Mathf.RoundToInt(timeSaved * 10f * 15f); // timeSaved * (tenths of second) * (points per tenth)

        // Final Score = (Kill Score * Accuracy Multiplier) + Time Bonus
        currentScore = Mathf.RoundToInt(enemyKillScore * accuracyMultiplier) + timeBonus;

        Debug.Log($"score: kills: {enemyKillScore} * accuracy ({accuracy:P2}) + time bonus ({timeSaved:F1}s saved): {timeBonus} = Total: {currentScore}");
    
        // Update the UI only at the end
        UpdateUI(); 
    }

    void UpdateUI()
    {
        if (scoreText == null) return;
        scoreText.text = currentScore.ToString();
    }

    // Optional: Call this if enemies can spawn mid-level
    public void RegisterEnemy()
    {
        enemiesTotal++;
        // No UpdateUI here
    }
}
