using UnityEditor;
using UnityEngine;

public sealed class ArenaGrassImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath != "Assets/Game/Art/Environment/GrassRepeat.png") return;
        var texture = (TextureImporter)assetImporter;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.mipmapEnabled = true;
        texture.alphaSource = TextureImporterAlphaSource.None;
        texture.isReadable = false;
        texture.maxTextureSize = 1024;
    }
}
