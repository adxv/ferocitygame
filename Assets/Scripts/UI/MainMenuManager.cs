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
    public Slider volumeSlider;
    public Button backButton;
    
    private bool isInOptionsMenu = false;
    
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
        
        // Add listener to volume slider
        volumeSlider.onValueChanged.AddListener(SetVolume);
        
        // Initialize volume slider to current value
        volumeSlider.value = AudioListener.volume;
    }
    
    private void ShowOptionsMenu()
    {
        isInOptionsMenu = true;
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    
    private void HideOptionsMenu()
    {
        isInOptionsMenu = false;
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    private void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        // You may want to save this value in PlayerPrefs
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
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