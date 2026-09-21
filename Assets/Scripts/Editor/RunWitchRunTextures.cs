using UnityEditor;
using UnityEngine;

public class RunWitchRunTextures : AssetPostprocessor
{
    public const float PixelsPerUnit = 16f;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/") || !assetImporter.importSettingsMissing)
        {
            return;
        }
        Configure((TextureImporter)assetImporter, assetPath);
    }

    public static bool NeedsConfigure(TextureImporter importer)
    {
        return importer.textureType != TextureImporterType.Sprite
            || importer.filterMode != FilterMode.Point
            || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit);
    }

    public static void Configure(TextureImporter importer, string path)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        bool bottomPivot = path.Contains("/Characters/") || path.Contains("/Obstacles/") || path.Contains("ground_plants");
        settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        importer.SetTextureSettings(settings);

        if (path.EndsWith("ui_button.png"))
        {
            importer.spriteBorder = new Vector4(4f, 4f, 4f, 4f);
        }
        else if (path.EndsWith("ui_panel.png"))
        {
            importer.spriteBorder = new Vector4(6f, 6f, 6f, 6f);
        }
    }
}
