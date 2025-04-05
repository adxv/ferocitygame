using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class RetroLightManager : MonoBehaviour
{
    [MenuItem("Game/Apply Retro Lighting Settings")]
    public static void ApplyRetroLightingSettings()
    {
        // Find all the 2D lights in the scene
        Light2D[] lights = FindObjectsOfType<Light2D>();
        
        if (lights.Length == 0)
        {
            Debug.LogWarning("No Light2D components found in the scene!");
            return;
        }
        
        Debug.Log($"Found {lights.Length} lights to modify");
        
        // Configure each light for a retro look
        foreach (Light2D light in lights)
        {
            // Adjust falloff for a harder edge (more pixelated appearance)
            if (light.lightType == Light2D.LightType.Point)
            {
                // For point lights, make sharper falloff
                light.falloffIntensity = 0.8f;
                
                // Make inner radius closer to outer radius for a sharper edge
                float outerRadius = light.pointLightOuterRadius;
                light.pointLightInnerRadius = outerRadius * 0.8f;
            }
            else if (light.lightType == Light2D.LightType.Freeform)
            {
                // For shape lights, make the falloff smaller
                light.falloffIntensity = 0.7f;
                light.shapeLightFalloffSize = Mathf.Max(0.1f, light.shapeLightFalloffSize * 0.5f);
            }
            
            // Increase contrast
            light.intensity = Mathf.Clamp(light.intensity * 1.2f, 0.5f, 2f);
            
            EditorUtility.SetDirty(light);
        }
        
        // Apply URP settings if possible
        TryConfigureURPSettings();
        
        Debug.Log("Retro light settings applied successfully!");
    }
    
    private static void TryConfigureURPSettings()
    {
        // Get the current URP asset
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            Debug.LogError("URP not found! This script requires the Universal Render Pipeline.");
            return;
        }
        
        // Display instructions for manual URP configuration
        EditorUtility.DisplayDialog("URP Settings", 
            "For the best retro lighting effect, please make the following changes to your URP Asset:\n\n" +
            "1. Set 'Light Render Scale' to 0.25-0.5\n" +
            "2. Disable HDR if enabled\n" +
            "3. Set 'Anti Aliasing (MSAA)' to 'Off'\n" +
            "4. In '2D Renderer Data', set 'Light Blend Styles' to use 'Multiply' or 'Additive' modes\n\n" +
            "These settings cannot be applied via script and must be set manually.",
            "OK");
        
        // Select the URP asset in the Project window to make it easier to find
        Selection.activeObject = pipeline;
        EditorGUIUtility.PingObject(pipeline);
    }
}