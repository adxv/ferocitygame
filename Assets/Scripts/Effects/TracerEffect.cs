using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class TracerEffect : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public float lifetime = 0.1f; // How long the tracer is visible

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // Disable it initially until it's set up
        lineRenderer.enabled = false;
    }

    // Call this immediately after instantiating the tracer
    public void Setup(Vector3 startPoint, Vector3 endPoint)
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer not found on TracerEffect!");
            Destroy(gameObject); // Clean up if something is wrong
            return;
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.enabled = true;

        // Schedule destruction after lifetime
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        // Optional: Implement fading if desired
        // For now, just wait and destroy
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}