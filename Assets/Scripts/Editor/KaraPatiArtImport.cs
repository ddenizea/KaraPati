using UnityEditor;

namespace KaraPati.EditorTools
{
    /// <summary>
    /// Import settings for the hand-drawn pictures in Assets/Resources/Items and Assets/Resources/Cats:
    /// sprites, no mipmaps, transparent edges kept clean, at most 256 px.
    /// </summary>
    public class KaraPatiArtImport : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/Items/";
        const string CatFolder = "Assets/Resources/Cats/";
        const string IconFolder = "Assets/Resources/Icons/";
        const string UiFolder = "Assets/Resources/UI/";   // stretchable button and panel shapes: never compressed

        void OnPreprocessTexture()
        {
            bool ui = assetPath.StartsWith(UiFolder);
            if (!assetPath.StartsWith(Folder) && !assetPath.StartsWith(CatFolder) && !assetPath.StartsWith(IconFolder) && !ui) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 256;
            ti.textureCompression = ui ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            if (ui) ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
