using UnityEngine;
using TMPro; // Keep: Needed for TextMeshPro
using System.Collections.Generic; // Keep: Needed for potential future use
using System.Collections;

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

        // Tell UIManager that level is complete if available
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelComplete();
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
