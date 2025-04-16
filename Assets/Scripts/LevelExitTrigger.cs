using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LevelExitTrigger : MonoBehaviour
{
    // Visual indicator components
    [Header("Visual Indicators")]
    [Tooltip("Child object that indicates exit is active with animation")]
    public GameObject unlockedIndicator;
    
    // Trigger settings
    [Header("Trigger Settings")]
    public string scoreScreenName = "ScoreScreen"; // Changed from levelSelectSceneName
    public float activationDelay = 1.0f; // Delay after level completion before exit becomes active
    
    // Indicator animation settings
    [Header("Indicator Animation")]
    [Tooltip("Speed of the indicator animation")]
    public float indicatorBobSpeed = 2f;
    [Tooltip("Maximum movement distance (total range will be 2x this value)")]
    public float indicatorBobAmount = 0.2f;
    
    // Fade settings
    [Header("Fade Settings")]
    [Tooltip("Reference to the Canvas/Image used for screen fading")]
    public CanvasGroup fadeCanvasGroup;
    [Tooltip("Duration of the fade transition in seconds")]
    public float fadeDuration = 0.5f;
    
    // Private variables
    private bool isActive = false;
    private Collider2D triggerCollider;
    private bool isTransitioning = false;
    private Vector3 indicatorStartPosition;
    
    void Start()
    {
        // Get the collider component
        triggerCollider = GetComponent<Collider2D>();
        
        // Make sure this has a trigger collider
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning("LevelExitTrigger collider should be set as a trigger!");
            triggerCollider.isTrigger = true;
        }
        
        // Set up the UnlockedIndicator
        if (unlockedIndicator == null)
        {
            // Try to find it as a child
            unlockedIndicator = transform.Find("UnlockedIndicator")?.gameObject;
        }
        
        if (unlockedIndicator != null)
        {
            indicatorStartPosition = unlockedIndicator.transform.localPosition;
            unlockedIndicator.SetActive(false); // Start disabled
        }
        
        // Try to find the fade canvas if not assigned
        EnsureFadeCanvasGroup();
        
        // Initialize in inactive state
        SetTriggerState(false);
        
        // If level is already complete on start (shouldn't happen normally),
        // activate the trigger after the delay
        if (FloorAccessController.isLevelComplete)
        {
            Invoke("ActivateTrigger", activationDelay);
        }
    }
    
    void Update()
    {
        // Check if level just completed
        if (FloorAccessController.isLevelComplete && !isActive)
        {
            Invoke("ActivateTrigger", activationDelay);
        }
        
        // Animate the indicator if it's active
        if (unlockedIndicator != null && unlockedIndicator.activeSelf)
        {
            float newY = indicatorStartPosition.y + Mathf.Sin(Time.time * indicatorBobSpeed) * indicatorBobAmount;
            unlockedIndicator.transform.localPosition = new Vector3(
                indicatorStartPosition.x,
                newY,
                indicatorStartPosition.z
            );
        }
    }
    
    // Helper function to ensure we always have a reference to the fade canvas
    private void EnsureFadeCanvasGroup()
    {
        if (fadeCanvasGroup == null)
        {
            // First try to find by tag
            GameObject fadeCanvas = GameObject.FindWithTag("FadeCanvas");
            if (fadeCanvas != null)
            {
                fadeCanvasGroup = fadeCanvas.GetComponent<CanvasGroup>();
            }
            
            // If that didn't work, try to find any CanvasGroup
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = GameObject.FindObjectOfType<CanvasGroup>();
            }
            
            // If we found a canvasGroup, initialize it
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
                Debug.Log("Found and initialized fade canvas group");
            }
            else
            {
                Debug.LogWarning("Could not find any CanvasGroup for screen fading");
            }
        }
    }
    
    void ActivateTrigger()
    {
        SetTriggerState(true);
    }
    
    void SetTriggerState(bool active)
    {
        isActive = active;
        
        // Enable/disable collider
        if (triggerCollider != null)
        {
            triggerCollider.enabled = active;
        }
        
        // Update indicator visual
        if (unlockedIndicator != null)
        {
            unlockedIndicator.SetActive(active);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to player and only if active and not already transitioning
        if (isActive && !isTransitioning && other.CompareTag("Player"))
        {
            StartCoroutine(FadeAndReturnToLevelSelect());
        }
    }
    
    IEnumerator FadeAndReturnToLevelSelect()
    {
        // Prevent multiple triggers during transition
        isTransitioning = true;
        
        // Ensure we're not in a paused state
        Time.timeScale = 1f;
        
        // Store the current level name for potential retry
        PlayerPrefs.SetString("LastPlayedLevel", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        
        // Ensure we have a reference to the fade canvas
        EnsureFadeCanvasGroup();
        
        // Only attempt fade if we have a canvas group
        if (fadeCanvasGroup != null)
        {
            // Make sure the canvas is active
            fadeCanvasGroup.gameObject.SetActive(true);
            
            // Fade to black
            float startTime = Time.time;
            float endTime = startTime + fadeDuration;
            
            while (Time.time < endTime)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / fadeDuration;
                fadeCanvasGroup.alpha = normalizedTime;
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 1f; // Ensure we're fully black
            fadeCanvasGroup.blocksRaycasts = true;
            
            Debug.Log("Screen fade complete");
        }
        else
        {
            Debug.LogWarning("No fade canvas found - proceeding without screen fade");
        }
        
        // Give a small pause at full black
        yield return new WaitForSeconds(0.1f);
        
        // Hide UI elements
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.HideHUD();
            uiManager.HidePauseMenu();
            uiManager.HideLevelComplete();
            uiManager.HideGameOver();
        }
        
        // Get score data for the score screen
        if (ScoreManager.Instance != null)
        {
            // Transfer score data to ScoreScreenManager using the new properties
            ScoreScreenManager.KillsScore = ScoreManager.Instance.GetKillsScore();
            ScoreScreenManager.ComboBonus = ScoreManager.Instance.GetComboBonus();
            ScoreScreenManager.TimeBonus = ScoreManager.Instance.GetTimeBonus();
            ScoreScreenManager.Accuracy = ScoreManager.Instance.GetAccuracy();
            ScoreScreenManager.FinalScore = ScoreManager.Instance.GetCurrentScore();
            ScoreScreenManager.Grade = ScoreManager.Instance.GetGrade();
        }
        
        // Load the score screen instead of level select
        SceneManager.LoadScene(scoreScreenName);
    }
} 