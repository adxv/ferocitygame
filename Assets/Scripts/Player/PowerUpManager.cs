using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your power-up buttons here directly from the scene")]
    public List<Button> powerUpButtons = new List<Button>();
    [Tooltip("Button to return to level select screen")]
    public Button backButton;
    
    [Header("Power-Up Data")]
    public List<PowerUpData> availablePowerUps = new List<PowerUpData>();

    
    [Header("Scene Names")]
    [Tooltip("Name of the level select scene")]
    public string levelSelectSceneName = "LevelSelect";
    
    private string sceneToLoad;
    
    // Static properties to track selected power-up effects
    public static bool HasDoubleAmmo { get; private set; }
    public static bool HasDoubleHealth { get; private set; }
    public static float MovementSpeedMultiplier { get; private set; } = 1f;
    public static float AccuracyMultiplier { get; private set; } = 1f;
    
    [System.Serializable]
    public class PowerUpData
    {
        public string powerUpName;
        public string description;
        public Sprite icon;
        public PowerUpType type;
    }
    
    public enum PowerUpType
    {
        DoubleAmmo,
        DoubleHealth,
        SpeedBoost,
        AccuracyBoost
    }
    
    void Start()
    {
        // Initialize default values
        ResetPowerUps();
        
        // Get the scene to load from LevelSelectManager or PlayerPrefs
        sceneToLoad = PlayerPrefs.GetString("SelectedLevel", "");
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("No level selected! Make sure a level was selected before coming to this scene.");
        }
        
        // Setup power-up buttons
        SetupPowerUpButtons();
        
        // Setup back button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(GoBackToLevelSelect);
        }
    }
    
    private void SetupPowerUpButtons()
    {
        // Validate button and data count match
        if (powerUpButtons.Count != availablePowerUps.Count)
        {
            Debug.LogWarning($"Power-up button count ({powerUpButtons.Count}) doesn't match power-up data count ({availablePowerUps.Count})!");
        }
        
        // Setup each button
        for (int i = 0; i < Mathf.Min(powerUpButtons.Count, availablePowerUps.Count); i++)
        {
            Button button = powerUpButtons[i];
            PowerUpData powerUpData = availablePowerUps[i];
            int powerUpIndex = i; // Capture the index for lambda
            
            if (button == null) continue;
            
            // Set button text and description
            Transform nameText = button.transform.Find("NameText");
            Transform descText = button.transform.Find("DescriptionText");
            
            if (nameText != null)
            {
                nameText.GetComponent<TextMeshProUGUI>().text = powerUpData.powerUpName;
            }
            
            if (descText != null)
            {
                descText.GetComponent<TextMeshProUGUI>().text = powerUpData.description;
            }
            
            // Set button icon if available
            Image iconImage = button.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && powerUpData.icon != null)
            {
                iconImage.sprite = powerUpData.icon;
                iconImage.preserveAspect = true;
            }
            
            // Clear existing listeners and add new one
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                SelectAndApplyPowerUp(powerUpIndex);
            });
        }
    }
    
    private void SelectAndApplyPowerUp(int index)
    {
        if (index >= 0 && index < availablePowerUps.Count)
        {
            // Reset all power-ups first
            ResetPowerUps();
            
            // Apply the selected power-up
            PowerUpData selectedPowerUp = availablePowerUps[index];
            
            switch (selectedPowerUp.type)
            {
                case PowerUpType.DoubleAmmo:
                    HasDoubleAmmo = true;
                    break;
                case PowerUpType.DoubleHealth:
                    HasDoubleHealth = true;
                    break;
                case PowerUpType.SpeedBoost:
                    MovementSpeedMultiplier = 1.3f;
                    break;
                case PowerUpType.AccuracyBoost:
                    AccuracyMultiplier = 0.5f; // Lower number means better accuracy (spread)
                    break;
            }
            
            Debug.Log("Applied power-up: " + selectedPowerUp.powerUpName);
            
            // Start the game immediately
            LoadGameScene();
        }
    }
    
    private void GoBackToLevelSelect()
    {
        // Load level select scene directly
        SceneManager.LoadScene(levelSelectSceneName);
    }
    
    private void ResetPowerUps()
    {
        HasDoubleAmmo = false;
        HasDoubleHealth = false;
        MovementSpeedMultiplier = 1f;
        AccuracyMultiplier = 1f;
    }
    
    private void LoadGameScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("Loading game scene: " + sceneToLoad);
            
            // Load the game scene directly without transition
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No scene to load! Make sure a level was selected.");
        }
    }
    
    // Static method to reset power-ups between game sessions
    public static void ResetAllPowerUps()
    {
        HasDoubleAmmo = false;
        HasDoubleHealth = false;
        MovementSpeedMultiplier = 1f;
        AccuracyMultiplier = 1f;
    }
}
