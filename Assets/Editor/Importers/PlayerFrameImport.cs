using UnityEditor;

public sealed class PlayerFrameImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool idleOrMove = assetPath.StartsWith("Assets/Game/Resources/Animations/Player/", System.StringComparison.Ordinal);
        bool action = assetPath.StartsWith("Assets/Game/Art/Animations/Player/", System.StringComparison.Ordinal);
        bool sword = assetPath.StartsWith("Assets/Game/Art/Effects/SwordEnergy/", System.StringComparison.Ordinal);
        if (!idleOrMove && !action && !sword)
            return;
        var texture = (TextureImporter)assetImporter;
        texture.alphaIsTransparency = true;
        texture.mipmapEnabled = false;
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        texture.maxTextureSize = sword ? 256 : 512;
        if (action || sword)
        {
            texture.textureType = TextureImporterType.Sprite;
            texture.spriteImportMode = SpriteImportMode.Single;
            texture.spritePixelsPerUnit = 128;
        }
    }
}
