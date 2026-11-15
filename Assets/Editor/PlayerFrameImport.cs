using UnityEditor;

public sealed class PlayerFrameImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Game/Resources/PlayerAnimation/", System.StringComparison.Ordinal))
            return;
        var texture = (TextureImporter)assetImporter;
        texture.alphaIsTransparency = true;
        texture.mipmapEnabled = false;
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        texture.maxTextureSize = 512;
    }
}
