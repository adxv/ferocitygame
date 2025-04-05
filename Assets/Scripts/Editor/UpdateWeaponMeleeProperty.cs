using UnityEngine;
using UnityEditor;

public class UpdateWeaponMeleeProperty : MonoBehaviour
{
    [MenuItem("Game/Set Fists as Melee Weapon")]
    public static void SetFistsAsMelee()
    {
        // Try to find the FistsWeaponData asset
        WeaponData fistsData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapons/FistsWeaponData.asset");
        
        if (fistsData != null)
        {
            // Set it as a melee weapon
            fistsData.isMelee = true;
            fistsData.magazineSize = 0;
            
            // Mark it as dirty and save
            EditorUtility.SetDirty(fistsData);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Set {fistsData.weaponName} as a melee weapon successfully!");
        }
        else
        {
            Debug.LogError("Could not find FistsWeaponData. Make sure it's located at Assets/Data/Weapons/FistsWeaponData.asset");
        }
    }
    
    [MenuItem("Game/Configure Melee for All Weapons")]
    public static void ConfigureMeleeForAllWeapons()
    {
        // Find all WeaponData assets in the project
        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        
        Debug.Log($"Found {guids.Length} weapon data assets");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            
            if (weaponData != null)
            {
                // Check if it's likely a melee weapon by its name
                string name = weaponData.weaponName.ToLower();
                bool shouldBeMelee = name.Contains("fist") || 
                                    name.Contains("knife") ||
                                    name.Contains("sword") ||
                                    name.Contains("bat") ||
                                    name.Contains("club") ||
                                    name.Contains("melee");
                
                // Only update if it's not already correctly set
                if (shouldBeMelee != weaponData.isMelee)
                {
                    weaponData.isMelee = shouldBeMelee;
                    
                    // For melee weapons, set magazine size to 0
                    if (shouldBeMelee)
                    {
                        weaponData.magazineSize = 0;
                    }
                    
                    // Mark the asset as dirty
                    EditorUtility.SetDirty(weaponData);
                    Debug.Log($"Updated {weaponData.weaponName}: Set isMelee to {shouldBeMelee}");
                }
            }
        }
        
        // Save all changes
        AssetDatabase.SaveAssets();
        Debug.Log("All weapon melee properties updated!");
    }
}