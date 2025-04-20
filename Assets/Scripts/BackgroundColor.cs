using UnityEngine;

public class BackgroundColor : MonoBehaviour
{
    public Camera mainCamera;
    public Color[] colors;
    public float fadeDuration = 2f;

    private int currentColorIndex = 0;
    private float fadeTimer = 0f;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (colors.Length == 0)
        {
            colors = new Color[] { Color.black };
        }

        mainCamera.backgroundColor = colors[0];
    }

    void Update()
    {
        if (colors.Length < 2) return;

        fadeTimer += Time.deltaTime;
        float t = fadeTimer / fadeDuration;

        int nextColorIndex = (currentColorIndex + 1) % colors.Length;
        Color currentColor = colors[currentColorIndex];
        Color nextColor = colors[nextColorIndex];

        //fade
        mainCamera.backgroundColor = Color.Lerp(currentColor, nextColor, t); //a + (b - a) * t cool

        if (t >= 1f)
        {
            currentColorIndex = nextColorIndex;
            fadeTimer = 0f;
        }
    }
}