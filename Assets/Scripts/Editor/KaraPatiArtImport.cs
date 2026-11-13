using UnityEditor;

namespace KaraPati.EditorTools
{
    /// <summary>
    /// Import settings for the hand-drawn item pictures in Assets/Resources/Items:
    /// sprites, no mipmaps, transparent edges kept clean, at most 256 px.
    /// </summary>
    public class KaraPatiArtImport : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/Items/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 256;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
