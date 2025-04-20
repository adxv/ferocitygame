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
    
    private List<Enemy> enemies = new List<Enemy>();
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("map_") || scene.name.Contains("Level"))
        {
            Debug.Log("resetting timer");
            ResetTimer();
        }
        else
        {
            startTime = 0;
            stopTime = 0;
            isRunning = false;
            hasStarted = false;
            
            // reset timer
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
        
        // reset timer
        if (timerText != null)
        {
            timerText.text = "00.000";
        }
        
        // refresh enemies list
        enemies.Clear();
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemies.Add(enemy);
        }
    }
    
    void Start()
    {
        // find all enemies
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemies.Add(enemy);
        }
        // reset timer
        if (timerText != null)
        {
            timerText.text = "00.000";
        }
    }
    
    void Update()
    {
        if (isRunning)
        {
            float elapsedTime = Time.time - startTime;
            
            UpdateTimerDisplay(elapsedTime);
            
            CheckEnemiesStatus();
        }
    }
    
    // call when player first moves
    public void StartTimer()
    {
        if (!hasStarted)
        {
            startTime = Time.time;
            isRunning = true;
            hasStarted = true;
        }
    }
    
    // call when all enemies are defeated
    public void StopTimer()
    {
        if (isRunning)
        {
            stopTime = Time.time;
            isRunning = false;
            
            float elapsedTime = stopTime - startTime;
            UpdateTimerDisplay(elapsedTime);
        }
    }
    
    private void CheckEnemiesStatus()
    {
        // ff there are no enemies or all enemies are dead, stop the timer
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
        
        // calculate seconds and milliseconds
        int seconds = Mathf.FloorToInt(timeToDisplay);
        int milliseconds = Mathf.FloorToInt((timeToDisplay - seconds) * 1000);

        // format text
        timerText.text = string.Format("{0:00}.{1:000}", seconds, milliseconds);
    }

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