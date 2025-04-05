using UnityEngine;
using UnityEditor;

public class CreateShotgunPickup : MonoBehaviour
{
    [MenuItem("Game/Create Shotgun Pickup Prefab")]
    public static void CreateShotgunPickupPrefab()
    {
        // Load the Makarov pickup prefab to use as a template
        GameObject makarovPickup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WeaponPickup/MakarovPickup.prefab");
        if (makarovPickup == null)
        {
            Debug.LogError("Failed to load MakarovPickup prefab. Make sure it exists at Assets/Prefabs/WeaponPickup/MakarovPickup.prefab");
            return;
        }
        
        // Duplicate the prefab
        GameObject shotgunPickup = Object.Instantiate(makarovPickup);
        shotgunPickup.name = "ShotgunPickup";
        
        // Load the shotgun weapon data
        WeaponData shotgunData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapons/ShotgunWeaponData.asset");
        if (shotgunData == null)
        {
            Debug.LogError("Shotgun weapon data not found! Make sure to create it first using the 'Create Shotgun Weapon Data' menu item.");
            Object.DestroyImmediate(shotgunPickup);
            return;
        }
        
        // Update the WeaponPickup component to use the shotgun data
        WeaponPickup pickupComponent = shotgunPickup.GetComponent<WeaponPickup>();
        if (pickupComponent != null)
        {
            pickupComponent.weaponData = shotgunData;
        }
        
        // Make the prefab slightly larger to distinguish it
        shotgunPickup.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        
        // Save the prefab
        string prefabPath = "Assets/Prefabs/WeaponPickup/ShotgunPickup.prefab";
        GameObject createdPrefab = PrefabUtility.SaveAsPrefabAsset(shotgunPickup, prefabPath);
        
        // Clean up the temporary instance
        Object.DestroyImmediate(shotgunPickup);
        
        Debug.Log("Shotgun pickup prefab created at " + prefabPath);
        
        // Select the created prefab in the project view
        Selection.activeObject = createdPrefab;
        
        // Update the WeaponData to reference the pickup prefab
        if (shotgunData != null)
        {
            shotgunData.pickupPrefab = createdPrefab;
            EditorUtility.SetDirty(shotgunData);
            AssetDatabase.SaveAssets();
            Debug.Log("Updated ShotgunWeaponData with the pickup prefab reference");
        }
    }
}