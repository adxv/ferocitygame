using UnityEngine;
using UnityEditor;
using TMPro;

public class CreateAmmoDisplay : MonoBehaviour
{
    [MenuItem("Game/Create Ammo Display")]
    public static void CreateAmmoDisplayUI()
    {
        // Find the Canvas in the scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene. Please create a Canvas first.");
            return;
        }

        // Create a new TextMeshProUGUI element for ammo display
        GameObject ammoDisplayObj = new GameObject("AmmoDisplay");
        ammoDisplayObj.transform.SetParent(canvas.transform, false);
        
        // Add TextMeshProUGUI component
        TextMeshProUGUI textMesh = ammoDisplayObj.AddComponent<TextMeshProUGUI>();
        textMesh.text = "0 / 0";
        textMesh.fontSize = 36;
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignmentOptions.Right;
        
        // Set the RectTransform properties
        RectTransform rectTransform = ammoDisplayObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(1, 0);
        rectTransform.anchoredPosition = new Vector2(-50, 50);
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // Add the AmmoDisplay script
        AmmoDisplay ammoDisplay = ammoDisplayObj.AddComponent<AmmoDisplay>();
        
        // Try to find PlayerController and assign it
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            ammoDisplay.playerController = playerController;
        }
        else
        {
            Debug.LogWarning("PlayerController not found in the scene. You'll need to assign it manually.");
        }
        
        ammoDisplay.ammoText = textMesh;
        
        // Select the newly created object
        Selection.activeGameObject = ammoDisplayObj;
        
        Debug.Log("Ammo Display created successfully!");
    }
}