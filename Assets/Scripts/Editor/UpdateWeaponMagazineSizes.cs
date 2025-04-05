using UnityEngine;
using UnityEditor;

public class UpdateWeaponMagazineSizes : MonoBehaviour
{
    [MenuItem("Game/Update All Weapon Magazine Sizes")]
    public static void UpdateAllWeaponMagazines()
    {
        // Find all WeaponData assets in the project
        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            
            if (weaponData != null)
            {
                // Set magazine sizes based on weapon name or other properties
                SetReasonableMagazineSize(weaponData);
                
                // Mark the asset as dirty and save it
                EditorUtility.SetDirty(weaponData);
                Debug.Log($"Updated {weaponData.weaponName}: Magazine size set to {weaponData.magazineSize}");
            }
        }
        
        // Save all changes
        AssetDatabase.SaveAssets();
        Debug.Log("All weapon magazine sizes updated!");
    }
    
    private static void SetReasonableMagazineSize(WeaponData weapon)
    {
        // Set magazine size based on weapon name
        string name = weapon.weaponName.ToLower();
        
        // Skip updating fists or non-shooting weapons
        if (name.Contains("fist") || !weapon.canShoot)
        {
            weapon.magazineSize = 0;
            return;
        }
        
        // Check weapon type by name and set appropriate magazine size
        if (name.Contains("shotgun"))
        {
            weapon.magazineSize = 8;
        }
        else if (name.Contains("uzi") || name.Contains("smg"))
        {
            weapon.magazineSize = 30;
        }
        else if (name.Contains("makarov") || name.Contains("pistol"))
        {
            weapon.magazineSize = 12;
        }
        else
        {
            // Default magazine size for unknown weapons
            weapon.magazineSize = 15;
        }
        
        // Or you could set magazine sizes based on pellet count
        // if (weapon.pelletCount > 1) { weapon.magazineSize = 8; } // Shotgun
    }
}