using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneFadeEffect : MonoBehaviour
{
    public Image fadeImage;
    private float fadeDuration = 0.5f;
    private float fadeStartTime;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImage != null)
        {
            fadeStartTime = Time.time;
            fadeImage.color = new Color(0, 0, 0, 0.9f);
        }
    }

    void Update()
    {
        if (fadeImage != null && Time.time - fadeStartTime <= fadeDuration)
        {
            //fade
            float t = (Time.time - fadeStartTime) / fadeDuration;
            fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0.9f, 0f, t));
        }
        else if (Time.time - fadeStartTime > fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}