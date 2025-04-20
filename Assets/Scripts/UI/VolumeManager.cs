using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    public Slider volumeSlider;
    void Start()
    {
        if(!PlayerPrefs.HasKey("MasterVolume")) {
            PlayerPrefs.SetFloat("MasterVolume", 1.0f);
            LoadVolume();
        }
        else {
            LoadVolume();
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volumeSlider.value;

        SaveVolume();
    }
    private void SaveVolume() {
        PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
    }
    private void LoadVolume() {
        volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume");
    }
}
