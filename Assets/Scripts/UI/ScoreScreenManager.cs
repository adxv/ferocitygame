using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI killsValue;
    public TextMeshProUGUI comboBonusValue;
    public TextMeshProUGUI timeBonusValue;
    public TextMeshProUGUI accuracyValue;
    public TextMeshProUGUI finalScoreValue;
    public TextMeshProUGUI gradeValue;
    
    [Header("Navigation")]
    public Button retryButton;
    public Button backButton;
    public string levelSelectScene = "LevelSelect";
    
    // Score data to be transferred between scenes
    public static int KillsScore { get; set; }
    public static int ComboBonus { get; set; }
    public static int TimeBonus { get; set; }
    public static float Accuracy { get; set; }
    public static int FinalScore { get; set; }
    public static string Grade { get; set; } = "D";
    
    void Start()
    {
        // Setup UI with score data
        DisplayScoreInfo();
        
        // Setup button listeners
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
    }
    
    public void ContinueToLevelSelect()
    {
        SceneManager.LoadScene(levelSelectScene);
    }
    
    public void RetryLevel()
    {
        // Store the current level name to reload it
        string currentLevel = PlayerPrefs.GetString("LastPlayedLevel", "Level1");
        SceneManager.LoadScene(currentLevel);
    }
} 