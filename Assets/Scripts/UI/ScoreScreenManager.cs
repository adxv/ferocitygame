using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreScreenManager : MonoBehaviour
{
    public TextMeshProUGUI killsValue;
    public TextMeshProUGUI comboBonusValue;
    public TextMeshProUGUI timeBonusValue;
    public TextMeshProUGUI accuracyValue;
    public TextMeshProUGUI finalScoreValue;
    public TextMeshProUGUI gradeValue;
    public TextMeshProUGUI completionTimeValue;
    
    public Button retryButton;
    public Button backButton;
    public string levelSelectScene = "LevelSelect";
    
    public static int KillsScore { get; set; }
    public static int ComboBonus { get; set; }
    public static int TimeBonus { get; set; }
    public static float Accuracy { get; set; }
    public static int FinalScore { get; set; }
    public static string Grade { get; set; } = "D";
    public static float CompletionTime { get; set; }
    
    void Start()
    {
        DisplayScoreInfo();
        
        if (backButton != null)
            backButton.onClick.AddListener(ContinueToLevelSelect);
            
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryLevel);
    }
    
    void DisplayScoreInfo()
    {
        if (killsValue != null)
            killsValue.text = KillsScore.ToString();
            
        if (comboBonusValue != null)
            comboBonusValue.text = ComboBonus.ToString();
            
        if (timeBonusValue != null)
            timeBonusValue.text = TimeBonus.ToString();
            
        if (accuracyValue != null)
            accuracyValue.text = (Accuracy * 100).ToString("0.0");
            
        if (finalScoreValue != null)
            finalScoreValue.text = FinalScore.ToString();
            
        if (gradeValue != null)
            gradeValue.text = Grade;
            
        if (completionTimeValue != null)
            completionTimeValue.text = CompletionTime.ToString("0.00") + "s";
    }
    
    public void ContinueToLevelSelect()
    {
        // find and reset the fade canvas alpha
        CanvasGroup fadeCanvas = FindFirstObjectByType<CanvasGroup>();
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
        
        SceneManager.LoadScene(levelSelectScene);
    }
    
    public void RetryLevel()
    {
        FloorAccessController.isLevelComplete = false;
        
        // find and reset the fade canvas alpha
        CanvasGroup fadeCanvas = FindFirstObjectByType<CanvasGroup>();
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
        
        string currentLevel = PlayerPrefs.GetString("LastPlayedLevel", "Level1");
        SceneManager.LoadScene(currentLevel);
    }
} 