using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public List<Button> menuButtons;
    public Image controlsImage;
    
    public Button backButton;
    
    private void ResetGameState()
    {
        FloorAccessController.isLevelComplete = false;
    }
    
    private void Start()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        
        if (menuButtons.Count >= 3)
        {
            menuButtons[0].onClick.AddListener(() => {  // lambda function, have no idea about this syntax
                ResetGameState();
                SceneManager.LoadScene("LevelSelect");
            });
            
            menuButtons[1].onClick.AddListener(() => {
                ShowOptionsMenu();
            });
            
            menuButtons[2].onClick.AddListener(() => {
                QuitGame();
            });
        }
        
        //back button
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
        Application.Quit();
        
        // stops play mode in editor
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
} 