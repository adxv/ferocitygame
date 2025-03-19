using UnityEditor;
using UnityEngine;

public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter textureImporter = assetImporter as TextureImporter;
        if (textureImporter != null && textureImporter.assetPath.Contains(".png")) // Adjust for your sprite file types
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spritePixelsPerUnit = 64; // Correct property
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed; // Correct property
        }
    }
}