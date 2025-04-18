using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class TimerController : MonoBehaviour
{
    public TMP_Text timerText;
    
    private float startTime;
    private float stopTime;
    private bool isRunning = false;
    private bool hasStarted = false;
    
    // Reference to track all enemies
    private List<Enemy> enemies = new List<Enemy>();
    
    private void OnEnable()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only reset timer for gameplay scenes (not menus)
        if (scene.name.Contains("map_") || scene.name.Contains("Level"))
        {
            Debug.Log("TimerController: Resetting timer for scene: " + scene.name);
            ResetTimer();
        }
        else
        {
            // For non-gameplay scenes, make sure the timer is still in a clean state
            startTime = 0;
            stopTime = 0;
            isRunning = false;
            hasStarted = false;
            
            // Reset timer display
            if (timerText != null)
            {
                timerText.text = "00.000";
            }
        }
    }

    private void ResetTimer()
    {
        startTime = 0;
        stopTime = 0;
        isRunning = false;
        hasStarted = false;
        
        // Reset timer display
        if (timerText != null)
        {
            timerText.text = "00.000";
        }
        
        // Clear and refind enemies
        enemies.Clear();
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemies.Add(enemy);
        }
    }
    
    void Start()
    {
        // Find all enemies in the scene at start
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemies.Add(enemy);
        }
        
        // Make sure timer text is initialized but empty
        if (timerText != null)
        {
            timerText.text = "00.000";
        }
    }
    
    void Update()
    {
        if (isRunning)
        {
            // Calculate and display elapsed time
            float elapsedTime = Time.time - startTime;
            
            UpdateTimerDisplay(elapsedTime);
            
            // Check if all enemies are dead
            CheckEnemiesStatus();
        }
    }
    
    // Call this when player first moves
    public void StartTimer()
    {
        if (!hasStarted)
        {
            startTime = Time.time;
            isRunning = true;
            hasStarted = true;
        }
    }
    
    // Call this when all enemies are defeated
    public void StopTimer()
    {
        if (isRunning)
        {
            stopTime = Time.time;
            isRunning = false;
            
            // One final update to ensure accuracy
            float elapsedTime = stopTime - startTime;
            UpdateTimerDisplay(elapsedTime);
        }
    }
    
    // Register new enemies that spawn after the game starts
    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }
    
    private void CheckEnemiesStatus()
    {
        // If there are no enemies or all enemies are dead, stop the timer
        bool allDead = true;
        
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                allDead = false;
                break;
            }
        }
        
        if (allDead && enemies.Count > 0)
        {
            StopTimer();
        }
    }
    
    private void UpdateTimerDisplay(float timeToDisplay)
    {
        if (timerText == null) return;
        
        // Calculate seconds and milliseconds
        int seconds = Mathf.FloorToInt(timeToDisplay);
        int milliseconds = Mathf.FloorToInt((timeToDisplay - seconds) * 1000);
        
        // Update the TMP Text with the formatted time
        timerText.text = string.Format("{0:00}.{1:000}", seconds, milliseconds);
    }

    // Get the current time (for UIManager)
    public float GetCurrentTime()
    {
        if (isRunning)
        {
            return Time.time - startTime;
        }
        else if (hasStarted)
        {
            return stopTime - startTime;
        }
        return 0f;
    }
}