using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class LevelSelectManager : MonoBehaviour
{
    public GameObject levelButtonPrefab;
    public Transform levelButtonsContainer;
    public Button backButton;
    
    public List<LevelData> availableLevels = new List<LevelData>();
    
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.5f;
    
    private int selectedLevelIndex = 0;
    private string sceneToLoad;

    public static bool shouldFadeInOnLoad = false;
    
    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public string sceneName;
        public Sprite levelPreview;
        public bool isLocked = false;   //unused for now
    }
    
    void Start()
    {
        CreateLevelButtons();
        
        // back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                SceneManager.LoadScene("MainMenu");
            });
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
            //set text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = availableLevels[i].levelName;
            }
            
            // set preview
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && availableLevels[i].levelPreview != null)
            {
                buttonImage.sprite = availableLevels[i].levelPreview;
                buttonImage.preserveAspect = true;
            }
            
            // Handle locked levels, unused
            if (availableLevels[i].isLocked)
            {
                button.interactable = false;
                //add lock ???
            }
            
            // click listener
            button.onClick.AddListener(() => {
                SelectLevel(levelIndex);
                LoadSelectedLevel();
            });
        }
    }
    
    private void SelectLevel(int index)
    {
        selectedLevelIndex = index;
        
        for (int i = 0; i < levelButtonsContainer.childCount; i++)
        {
            Button button = levelButtonsContainer.GetChild(i).GetComponent<Button>();
        }
    }
    
    private void LoadSelectedLevel()
    {
        if (availableLevels.Count > 0 && selectedLevelIndex >= 0 && selectedLevelIndex < availableLevels.Count)
        {
            LevelData levelToLoad = availableLevels[selectedLevelIndex];
            
            if (!levelToLoad.isLocked)
            {
                // reset before loading level
                FloorAccessController.isLevelComplete = false;
                
                
                sceneToLoad = levelToLoad.sceneName;
                PlayerPrefs.SetString("SelectedLevel", sceneToLoad);
                PlayerPrefs.Save();
                
                SceneManager.LoadScene("PowerUpSelect");
            }
        }
    }
} 