using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject levelButtonPrefab;
    public Transform levelButtonsContainer;
    public Button backButton;
    
    [Header("Level Data")]
    public List<LevelData> availableLevels = new List<LevelData>();
    
    private int selectedLevelIndex = 0;
    
    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public string sceneName;
        public Sprite levelPreview;
        public bool isLocked = false;
    }
    
    void Start()
    {
        // Create level buttons based on available levels
        CreateLevelButtons();
        
        // Add listener to back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                SceneManager.LoadScene("MainMenu");
            });
        }
        
        // Select first level by default
        if (availableLevels.Count > 0)
        {
            SelectLevel(0);
        }
    }
    
    void Update()
    {
        // Handle keyboard/gamepad navigation
        HandleInput();
    }
    
    private void HandleInput()
    {
        // Navigate left/right between levels
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            selectedLevelIndex--;
            if (selectedLevelIndex < 0) selectedLevelIndex = availableLevels.Count - 1;
            SelectLevel(selectedLevelIndex);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            selectedLevelIndex++;
            if (selectedLevelIndex >= availableLevels.Count) selectedLevelIndex = 0;
            SelectLevel(selectedLevelIndex);
        }
        
        // Select current level
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (availableLevels.Count > 0 && !availableLevels[selectedLevelIndex].isLocked)
            {
                LoadSelectedLevel();
            }
        }
        
        // Go back to main menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    private void CreateLevelButtons()
    {
        // Clear any existing buttons
        foreach (Transform child in levelButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create new buttons for each level
        for (int i = 0; i < availableLevels.Count; i++)
        {
            int levelIndex = i; // Capture the index for the lambda
            
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonsContainer);
            Button button = buttonObj.GetComponent<Button>();
            
            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = availableLevels[i].levelName;
            }
            
            // Set button image if available
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && availableLevels[i].levelPreview != null)
            {
                buttonImage.sprite = availableLevels[i].levelPreview;
                buttonImage.preserveAspect = true;
            }
            
            // Handle locked levels
            if (availableLevels[i].isLocked)
            {
                button.interactable = false;
                // Add lock icon or visual indicator
                GameObject lockIcon = new GameObject("LockIcon");
                lockIcon.transform.SetParent(buttonObj.transform, false);
                Image lockImage = lockIcon.AddComponent<Image>();
                // Set lock icon sprite here
            }
            
            // Add click listener
            button.onClick.AddListener(() => {
                SelectLevel(levelIndex);
                LoadSelectedLevel();
            });
        }
    }
    
    private void SelectLevel(int index)
    {
        selectedLevelIndex = index;
        
        // Highlight the selected button
        for (int i = 0; i < levelButtonsContainer.childCount; i++)
        {
            Button button = levelButtonsContainer.GetChild(i).GetComponent<Button>();
            ColorBlock colors = button.colors;
            
            if (i == selectedLevelIndex)
            {
                colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
                button.colors = colors;
            }
            else
            {
                colors.normalColor = Color.white;
                button.colors = colors;
            }
        }
    }
    
    private void LoadSelectedLevel()
    {
        if (availableLevels.Count > 0 && selectedLevelIndex >= 0 && selectedLevelIndex < availableLevels.Count)
        {
            LevelData levelToLoad = availableLevels[selectedLevelIndex];
            
            if (!levelToLoad.isLocked)
            {
                Debug.Log("Loading level: " + levelToLoad.sceneName);
                SceneManager.LoadScene(levelToLoad.sceneName);
            }
        }
    }
} 