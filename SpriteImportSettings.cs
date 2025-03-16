using UnityEditor;
using UnityEngine;

public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Only apply to textures (PNG files in your case)
        if (assetPath.Contains(".png"))
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;

            // Only modify if it's a new import (not reimporting with existing settings)
            if (!textureImporter.isReadable) // A simple check to avoid overriding manual changes
            {
                // Set to Sprite (2D and UI)
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;

                // Set Filter Mode to Point
                textureImporter.filterMode = FilterMode.Point;

                // Set Compression to None
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;

                // Optional: Set Pixels Per Unit to 64 (for your 64 sprites)
                textureImporter.spritePixelsPerUnit = 64;

                // Apply changes
                textureImporter.SaveAndReimport();
            }
        }
    }
}