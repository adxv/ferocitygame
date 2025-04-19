using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Items")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public List<Button> menuButtons; // Ordered: New Game, Options, Exit
    public Image controlsImage;
    
    [Header("Options Settings")]
    public Button backButton;
    
    // Helper method to reset static game variables
    private void ResetGameState()
    {
        // Reset FloorAccessController variables
        FloorAccessController.isLevelComplete = false;
        
        // Reset any other static variables that need to be reset between levels
        // This ensures a clean state when starting a new level
    }
    
    private void Start()
    {
        // Hide options panel initially
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        
        // Add listeners to buttons
        if (menuButtons.Count >= 3)
        {
            // New Game button
            menuButtons[0].onClick.AddListener(() => {
                // Reset game state before loading level select
                ResetGameState();
                // Load level select scene - replace with your actual scene name
                SceneManager.LoadScene("LevelSelect");
            });
            
            // Options button
            menuButtons[1].onClick.AddListener(() => {
                ShowOptionsMenu();
            });
            
            // Exit button
            menuButtons[2].onClick.AddListener(() => {
                QuitGame();
            });
        }
        
        // Add listener to back button
        backButton.onClick.AddListener(() => {
            HideOptionsMenu();
        });
    }
    
    private void ShowOptionsMenu()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    
    private void HideOptionsMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
    
    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
        
        // In the Unity Editor, this line will stop play mode
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
} 