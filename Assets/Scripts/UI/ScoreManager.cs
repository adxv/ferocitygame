using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.AI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public TextMeshProUGUI scoreText;

    public float comboTimeWindow = 3f; //combo expiration time
    public float comboMultiplierBase = 1.5f;

    private int baseKillScore = 100;
    private int shotsFired = 0;
    private int shotsHit = 0;
    private int enemiesTotal = 0;
    private int enemiesDefeated = 0;
    private int currentScore = 0;
    private int killsScore = 0;
    private int comboBonus = 0;
    private int timeBonus = 0;
    private float startTime = 0f;
    private float endTime = 0f;
    private bool levelActive = false;

    float targetTime;
   
    private int currentCombo = 0;
    private float lastKillTime = 0f;
    private Coroutine comboTimerCoroutine;

    private string currentGrade = "D";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        } // prevent multiple instances of the ScoreManager
        else
        {
            Instance = this;
            
            // Make the ScoreManager persist between scenes
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetState();
        
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
        
        // stop combo timer
        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
            comboTimerCoroutine = null;
        }
    }

    void Start()
    {
        if (scoreText != null)
        {
            scoreText.text = "0";
        }
        
        Invoke("StartLevel", 0.1f); //0.1s delay for safety
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
        enemiesTotal = allEnemies.Length; //count enemies in scene
        targetTime = Mathf.Max(30f, enemiesTotal * 6f); // level completion target time, minimum 30 seconds, or 6 seconds per enemy

        Debug.Log("enemies found:" + enemiesTotal);

        if (scoreText != null)
        {
            scoreText.text = ""; // clear score text
        }
    }

    public void RecordShotFired()
    {
        if (!levelActive) return;
        shotsFired++;
        Debug.Log($"Shot fired! Total: {shotsFired}, Accuracy: {GetAccuracy():P2}");
    }

    public void RecordHit()
    {
        if (!levelActive) return;
        shotsHit++;
        Debug.Log($"HIT! Total hits: {shotsHit}/{shotsFired}, Accuracy: {GetAccuracy():P2}");
    }

    public void LogMissedShot()
    {
        if (!levelActive) return;
        Debug.Log($"MISSED! Hits: {shotsHit}/{shotsFired}, Accuracy: {GetAccuracy():P2}");
    }

    public void RecordEnemyDefeated()
    {
        if (!levelActive) return;
        
        // increase combo
        currentCombo++;
        lastKillTime = Time.time;
        
        // calculate kill score with combo multiplier
        float comboMultiplier = 1f + (currentCombo - 1) * 0.1f; // 10% increase per combo
        int scoreForKill = Mathf.RoundToInt(baseKillScore * comboMultiplier);
        
        killsScore += scoreForKill;
        comboBonus += scoreForKill - baseKillScore;
        
        // update total
        currentScore = killsScore;
        
        UpdateScoreUI();
        

        // combo timer
        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
        }
        comboTimerCoroutine = StartCoroutine(ComboTimer());
        
        enemiesDefeated++;
        
        if (enemiesDefeated >= enemiesTotal)
        {
            EndLevel();
        }
    }
    
    private IEnumerator ComboTimer()
    {
        yield return new WaitForSeconds(comboTimeWindow);
        
        if (Time.time - lastKillTime >= comboTimeWindow)
        {
            Debug.Log("combo expired");
            currentCombo = 0;
        }
    }

    void EndLevel()
    {
        endTime = Time.time;
        levelActive = false;
        CalculateFinalScore();
        Debug.Log($"level end - Final Accuracy: {GetAccuracy():P2}");

        // Store values in static variables to ensure they're accessible even if the instance is destroyed
        ScoreScreenManager.KillsScore = killsScore;
        ScoreScreenManager.ComboBonus = comboBonus;
        ScoreScreenManager.TimeBonus = timeBonus;
        ScoreScreenManager.Accuracy = GetAccuracy();
        ScoreScreenManager.FinalScore = currentScore;
        ScoreScreenManager.Grade = currentGrade;
        ScoreScreenManager.CompletionTime = endTime - startTime;

        // find all MonoBehaviour objects in the scene
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        bool levelCompleteShown = false;

        // mark the level as complete in the FloorAccessController
        FloorAccessController.isLevelComplete = true;

        // iterate through all MonoBehaviour objects to find one with a "levelCompleteScreen" field
        foreach (var behaviour in allMonoBehaviours)
        {
            //get all public instance fields of the current MonoBehaviour
            var fields = behaviour.GetType().GetFields(System.Reflection.BindingFlags.Public | 
                                  System.Reflection.BindingFlags.Instance); //i forgot what this does

            // check each field to see if it matches the name "levelCompleteScreen"
            foreach (var field in fields)
            {
            if (field.Name == "levelCompleteScreen")
            {
                // if found, send a message to the object to invoke the "ShowLevelComplete" method
                behaviour.SendMessage("ShowLevelComplete", null, SendMessageOptions.DontRequireReceiver);
                Debug.Log($"showLevelComplete to {behaviour.name}");
                levelCompleteShown = true;
                break;
            }
        }
            if (levelCompleteShown)
                break;
        }
        
        if (!levelCompleteShown)
        {
            Debug.LogWarning("could not find levelCompleteScreen");
        }
    }

    void CalculateFinalScore() //rewrite??????
    {
        float elapsedTime = endTime - startTime;
        float accuracy = GetAccuracy();

        // time bonus calculation
        // dynamic target time based on enemy count 6 seconds per enemy
        float timeSaved = Mathf.Max(0, targetTime - elapsedTime);
        timeBonus = Mathf.RoundToInt(timeSaved * 5f);

        //accuracy hard limit
        if(accuracy > 1.0f)
        {
            accuracy = 1.0f;
        }

        float accuracyMultiplier = Mathf.Max(0.1f, accuracy);
        int scoreWithAccuracy = Mathf.RoundToInt((killsScore + comboBonus) * accuracyMultiplier);
        
        currentScore = scoreWithAccuracy + timeBonus;
        
        // calculate grade (mostly based on accuracy)
        CalculateGrade(accuracy, elapsedTime);

        Debug.Log($"target time: {targetTime} ");
    }
    
    private void CalculateGrade(float accuracy, float completionTime)
    {
        // accuracy weight 80%
        // time weight 20%
        
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
        
        float exceptionalTime = targetTime * 0.6f; // 60% of target time is exceptional
        float penaltyTime = targetTime * 1.5f; // 150% of target time triggers grade penalty
        
        if (completionTime < exceptionalTime && currentGrade != "SS") // exceptional time
        {
            //bump grade up
            string[] grades = { "D", "C", "B", "A", "S", "SS" };
            int currentIndex = System.Array.IndexOf(grades, currentGrade);
            if (currentIndex < grades.Length - 1)
            {
                currentGrade = grades[currentIndex + 1];
            }
        }
        else if (completionTime > penaltyTime && currentGrade != "D") // exceptionally bad time
        {
            // bump grade down
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
    
    public int GetShotsFired()
    {
        return shotsFired;
    }
    
    public int GetShotsHit()
    {
        return shotsHit;
    }
}
