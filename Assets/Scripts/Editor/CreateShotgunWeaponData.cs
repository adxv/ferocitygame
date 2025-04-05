using UnityEngine;
using UnityEditor;

public class CreateShotgunWeaponData : MonoBehaviour
{
    [MenuItem("Game/Create Shotgun Weapon Data")]
    public static void CreateShotgunData()
    {
        // Create a new WeaponData asset
        WeaponData shotgunData = ScriptableObject.CreateInstance<WeaponData>();
        
        // Configure the shotgun data
        shotgunData.weaponName = "Shotgun";
        shotgunData.canShoot = true;
        
        // Find the bulletPrefab from an existing weapon
        WeaponData makarovData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapons/MakarovWeaponData.asset");
        if (makarovData != null)
        {
            shotgunData.projectilePrefab = makarovData.projectilePrefab;
            shotgunData.shootSound = makarovData.shootSound;
            shotgunData.playerSprite = makarovData.playerSprite;
        }
        
        // Set shotgun-specific properties
        shotgunData.fireRate = 2f;
        shotgunData.isFullAuto = false;
        shotgunData.damage = 1f;
        shotgunData.bulletOffset = 2f;
        shotgunData.bulletOffsetSide = 0f;
        shotgunData.pelletCount = 8;
        shotgunData.spreadAngle = 30f;
        shotgunData.shootShakeDuration = 0.15f;
        shotgunData.shootShakeMagnitude = 0.15f;
        
        // Save the asset
        AssetDatabase.CreateAsset(shotgunData, "Assets/Data/Weapons/ShotgunWeaponData.asset");
        AssetDatabase.SaveAssets();
        
        Debug.Log("Shotgun weapon data created at Assets/Data/Weapons/ShotgunWeaponData.asset");
        
        // Highlight the created asset in the Project view
        Selection.activeObject = shotgunData;
    }
}