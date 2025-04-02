using UnityEngine;

public class BackgroundColor : MonoBehaviour
{
    public Camera mainCamera; // Assign in Inspector
    public Color[] colors; // Array of colors to fade between
    public float fadeDuration = 2f; // Time (seconds) for each fade

    private int currentColorIndex = 0;
    private float fadeTimer = 0f;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Auto-find main camera if not assigned
        }

        if (colors.Length == 0)
        {
            Debug.LogError("No colors assigned to CameraBackgroundFader!");
            colors = new Color[] { Color.black }; // Default to black if empty
        }

        mainCamera.backgroundColor = colors[0]; // Set initial color
    }

    void Update()
    {
        if (colors.Length < 2) return; // Need at least 2 colors to fade

        fadeTimer += Time.deltaTime;
        float t = fadeTimer / fadeDuration; // Progress (0 to 1)

        // Get current and next color
        int nextColorIndex = (currentColorIndex + 1) % colors.Length;
        Color currentColor = colors[currentColorIndex];
        Color nextColor = colors[nextColorIndex];

        // Fade between colors
        mainCamera.backgroundColor = Color.Lerp(currentColor, nextColor, t);

        // Move to next color when fade is complete
        if (t >= 1f)
        {
            currentColorIndex = nextColorIndex;
            fadeTimer = 0f; // Reset timer
        }
    }
}