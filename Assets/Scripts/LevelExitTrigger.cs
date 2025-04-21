using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LevelExitTrigger : MonoBehaviour
{
    public GameObject unlockedIndicator;
    
    public string scoreScreenName = "ScoreScreen";
    public float activationDelay = 1.0f;
    
    public float indicatorBobSpeed = 2f;
    public float indicatorBobAmount = 0.2f;
    
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.5f;
    
    private bool isActive = false;
    private Collider2D triggerCollider;
    private bool isTransitioning = false;
    private Vector3 indicatorStartPosition;
    
    void Start()
    {
        // get collider
        triggerCollider = GetComponent<Collider2D>();
        
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }
        if (unlockedIndicator == null)
        {
            unlockedIndicator = transform.Find("UnlockedIndicator")?.gameObject;
        }
        
        if (unlockedIndicator != null)
        {
            indicatorStartPosition = unlockedIndicator.transform.localPosition;
            unlockedIndicator.SetActive(false); // start disabled
        }
        
        EnsureFadeCanvasGroup();
        SetTriggerState(false);
        
        if (FloorAccessController.isLevelComplete)
        {
            Invoke("ActivateTrigger", activationDelay);
        }
    }
    
    void Update()
    {
        // check if level completed
        if (FloorAccessController.isLevelComplete && !isActive)
        {
            Invoke("ActivateTrigger", activationDelay);
        }
        
        // animate indicator
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
    // keep reference even after restart
    private void EnsureFadeCanvasGroup()
    {
        if (fadeCanvasGroup == null)
        {
            GameObject fadeCanvas = GameObject.FindWithTag("FadeCanvas");
            if (fadeCanvas != null)
            {
                fadeCanvasGroup = fadeCanvas.GetComponent<CanvasGroup>();
            }
            
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                Debug.LogWarning("Could not find CanvasGroup");
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
        
        // enable/disable collider
        if (triggerCollider != null)
        {
            triggerCollider.enabled = active;
        }
        
        if (unlockedIndicator != null)
        {
            unlockedIndicator.SetActive(active);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive && !isTransitioning && other.CompareTag("Player"))
        {
            StartCoroutine(FadeAndReturnToLevelSelect());
        }
    }
    
    IEnumerator FadeAndReturnToLevelSelect()
    {
        isTransitioning = true;
        Time.timeScale = 1f;
        
        // store level name
        PlayerPrefs.SetString("LastPlayedLevel", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        
        EnsureFadeCanvasGroup();
        
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            
            // fade to black
            float startTime = Time.time;
            float endTime = startTime + fadeDuration;
            
            while (Time.time < endTime)
            {
                float elapsed = Time.time - startTime;
                float normalizedTime = elapsed / fadeDuration;
                fadeCanvasGroup.alpha = normalizedTime;
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }
        yield return new WaitForSeconds(0.1f);
        
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.HideHUD();
            uiManager.HidePauseMenu();
            uiManager.HideLevelComplete();
            uiManager.HideGameOver();
        }
        
        // get score data for the score screen
        if (ScoreManager.Instance != null)
        {
            ScoreScreenManager.KillsScore = ScoreManager.Instance.GetKillsScore();
            ScoreScreenManager.ComboBonus = ScoreManager.Instance.GetComboBonus();
            ScoreScreenManager.TimeBonus = ScoreManager.Instance.GetTimeBonus();
            ScoreScreenManager.Accuracy = ScoreManager.Instance.GetAccuracy();
            ScoreScreenManager.FinalScore = ScoreManager.Instance.GetCurrentScore();
            ScoreScreenManager.Grade = ScoreManager.Instance.GetGrade();
            ScoreScreenManager.CompletionTime = ScoreManager.Instance.GetElapsedTime();
        }
        else
        {
            Debug.LogWarning("LevelExitTrigger: ScoreManager.Instance is null!");
        }
        
        SceneManager.LoadScene(scoreScreenName);
    }
} 