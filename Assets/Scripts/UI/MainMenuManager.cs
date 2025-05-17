using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Audio;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public List<Button> menuButtons;
    public Image controlsImage;
    public AudioMixer mainMixer;

    public Button backButton;

    private void ResetGameState()
    {
        FloorAccessController.isLevelComplete = false;
    }

    private void Start()
    {
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float fxVolume = PlayerPrefs.GetFloat("FXVolume", 1.0f);

        mainMixer.SetFloat("MasterVolume", LinearToDecibel(masterVolume));
        mainMixer.SetFloat("MusicVolume", LinearToDecibel(musicVolume));
        mainMixer.SetFloat("FXVolume", LinearToDecibel(fxVolume));

        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        if (menuButtons.Count >= 3)
        {
            menuButtons[0].onClick.AddListener(() =>
            {  // lambda function, have no idea about this syntax
                ResetGameState();
                SceneManager.LoadScene("LevelSelect");
            });

            menuButtons[1].onClick.AddListener(() =>
            {
                ShowOptionsMenu();
            });

            menuButtons[2].onClick.AddListener(() =>
            {
                QuitGame();
            });
        }

        //back button
        backButton.onClick.AddListener(() =>
        {
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
        private float LinearToDecibel(float linear)
    {
        if (linear <= 0) return -80f;
        return 20f * Mathf.Log10(linear);
    }
} 