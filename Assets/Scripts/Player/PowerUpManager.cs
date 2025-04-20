using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class PowerUpManager : MonoBehaviour         //could rename to PerkManager
{
    public List<Button> powerUpButtons = new List<Button>();
    public Button backButton;
    
    public List<PowerUpData> availablePowerUps = new List<PowerUpData>();

    
    public string levelSelectSceneName = "LevelSelect";
    
    private string sceneToLoad;
    
    public static bool HasDoubleAmmo { get; private set; }
    public static bool HasDoubleHealth { get; private set; }
    public static float MovementSpeedMultiplier { get; private set; } = 1f;
    public static float AccuracyMultiplier { get; private set; } = 1f;
    
    [System.Serializable]
    public class PowerUpData
    {
        public string powerUpName;
        public PowerUpType type;
    }
    
    public enum PowerUpType
    {
        DoubleAmmo,
        DoubleHealth,
        SpeedBoost,
        AccuracyBoost,
        //Invincibility
    }
    
    void Start()
    {
        ResetPowerUps();
        
        //get scene
        sceneToLoad = PlayerPrefs.GetString("SelectedLevel", "");

        SetupPowerUpButtons();
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(GoBackToLevelSelect);
        }
    }
    
    private void SetupPowerUpButtons()
    {
        for (int i = 0; i < Mathf.Min(powerUpButtons.Count, availablePowerUps.Count); i++)
        {
            Button button = powerUpButtons[i];
            PowerUpData powerUpData = availablePowerUps[i];
            int powerUpIndex = i;
            
            if (button == null) continue;
            
            // clear existing listeners and add new one
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
            ResetPowerUps();
            
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
                    AccuracyMultiplier = 0.5f; // lower number = more accurate
                    break;
            }
            
            Debug.Log("perk: " + selectedPowerUp.powerUpName);
            
            LoadGameScene();
        }
    }
    
    private void GoBackToLevelSelect()
    {
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
            Debug.Log("scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("invalid level selected" + sceneToLoad);
        }
    }
    
    // static method to reset between game sessions
    public static void ResetAllPowerUps()
    {
        HasDoubleAmmo = false;
        HasDoubleHealth = false;
        MovementSpeedMultiplier = 1f;
        AccuracyMultiplier = 1f;
    }
}
