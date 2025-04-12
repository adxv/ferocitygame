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
    
    // Current selected button index
    private int selectedIndex = 0;
    private bool isInOptionsMenu = false;
    
    private void Start()
    {
        // Hide options panel initially
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        
        // Set initial selection
        SelectButton(0);
        
        // Add listeners to buttons
        if (menuButtons.Count >= 3)
        {
            // New Game button
            menuButtons[0].onClick.AddListener(() => {
                // Load level select scene - replace with your actual scene name
                SceneManager.LoadScene("map_residence");
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
    
    private void Update()
    {
        // Handle keyboard/gamepad navigation
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (isInOptionsMenu)
        {
            // In options menu, only handle Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideOptionsMenu();
                return;
            }
            
            // Navigate volume slider with left/right
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                volumeSlider.value -= 0.1f;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                volumeSlider.value += 0.1f;
            }
            
            // Return to main menu with Enter on back button
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HideOptionsMenu();
            }
            
            return;
        }
        
        // Main menu navigation
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            // Move up in the menu
            selectedIndex--;
            if (selectedIndex < 0) selectedIndex = menuButtons.Count - 1;
            SelectButton(selectedIndex);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            // Move down in the menu
            selectedIndex++;
            if (selectedIndex >= menuButtons.Count) selectedIndex = 0;
            SelectButton(selectedIndex);
        }
        
        // Select current option
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // Click the currently selected button
            menuButtons[selectedIndex].onClick.Invoke();
        }
        
        // Exit with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }
    
    private void SelectButton(int index)
    {
        // Deselect all buttons
        foreach (Button button in menuButtons)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            button.colors = colors;
            
            // Reset text color if using TextMeshPro
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = Color.white;
            }
        }
        
        // Select the specified button
        selectedIndex = index;
        Button selectedButton = menuButtons[selectedIndex];
        selectedButton.Select();
        
        // Change color of selected button
        ColorBlock selectedColors = selectedButton.colors;
        selectedColors.normalColor = new Color(0.9f, 0.9f, 0.9f);
        selectedButton.colors = selectedColors;
        
        // Change text color if using TextMeshPro
        TextMeshProUGUI selectedText = selectedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (selectedText != null)
        {
            selectedText.color = Color.yellow;
        }
    }
    
    private void ShowOptionsMenu()
    {
        isInOptionsMenu = true;
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        backButton.Select();
    }
    
    private void HideOptionsMenu()
    {
        isInOptionsMenu = false;
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        SelectButton(selectedIndex);
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