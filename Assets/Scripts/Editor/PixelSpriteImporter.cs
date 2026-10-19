// Auto-configures import settings for pixel art dropped into Assets/Resources/Sprites/ - Point
// filtering (no blur on 16x16 art), Sprite texture type, uncompressed (keeps hard pixel edges
// crisp), no mipmaps, and Pixels Per Unit = 16 to match TacticalGridView's TileWorldSize = 1f
// (so a 16x16 sprite exactly fills one world-unit tile). Runs automatically on import/reimport -
// no manual per-file setup after dropping in new art. Must live under an Editor/ folder.
using UnityEditor;
using UnityEngine;

public class PixelSpriteImporter : AssetPostprocessor
{
    private const string WatchedFolder = "Resources/Sprites/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains(WatchedFolder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
