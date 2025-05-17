using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    public AudioMixer mainMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider fxSlider;

    void Start()
    {
        if (!PlayerPrefs.HasKey("MasterVolume"))
        {
            PlayerPrefs.SetFloat("MasterVolume", 1.0f);
            PlayerPrefs.SetFloat("MusicVolume", 1.0f);
            PlayerPrefs.SetFloat("FXVolume", 1.0f);
        }
        LoadVolumes();
    }

    public void SetMasterVolume()
    {
        float volume = masterSlider.value;
        mainMixer.SetFloat("MasterVolume", LinearToDecibel(volume));
        SaveVolume("MasterVolume", volume);
    }

    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        mainMixer.SetFloat("MusicVolume", LinearToDecibel(volume));
        SaveVolume("MusicVolume", volume);
    }

    public void SetFXVolume()
    {
        float volume = fxSlider.value;
        mainMixer.SetFloat("FXVolume", LinearToDecibel(volume));
        SaveVolume("FXVolume", volume);
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    private void LoadVolumes()
    {
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float fxVolume = PlayerPrefs.GetFloat("FXVolume", 1.0f);

        masterSlider.value = masterVolume;
        musicSlider.value = musicVolume;
        fxSlider.value = fxVolume;

        mainMixer.SetFloat("MasterVolume", LinearToDecibel(masterVolume));
        mainMixer.SetFloat("MusicVolume", LinearToDecibel(musicVolume));
        mainMixer.SetFloat("FXVolume", LinearToDecibel(fxVolume));
    }

    private float LinearToDecibel(float linear)
    {
        if (linear <= 0) return -80f; // Mute at 0
        return 20f * Mathf.Log10(linear); // Convert linear (0-1) to dB (-80 to 0)
    }
}