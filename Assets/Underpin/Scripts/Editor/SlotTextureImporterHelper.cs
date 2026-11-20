using System.IO;
using UnityEditor;
using UnityEngine;

namespace Underpin.SlotMachine.Editor
{
    /// <summary>
    /// Editor utility to configure all textures in Assets/Underpin/Textures
    /// as Sprite (2D and UI) with optimal crisp rendering settings.
    /// </summary>
    public static class SlotTextureImporterHelper
    {
        [MenuItem("Underpin/Setup/1. Configure Textures as Sprites")]
        public static void ConfigureAllTextures()
        {
            string textureFolder = "Assets/Underpin/Textures";
            if (!Directory.Exists(textureFolder))
            {
                Debug.LogError($"[SlotTextureImporterHelper] Directory not found: {textureFolder}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder });
            int configuredCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool modified = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        modified = true;
                    }

                    if (importer.spritePixelsPerUnit != 100f)
                    {
                        importer.spritePixelsPerUnit = 100f;
                        modified = true;
                    }

                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        modified = true;
                    }

                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        modified = true;
                    }

                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        modified = true;
                    }

                    if (modified)
                    {
                        importer.SaveAndReimport();
                        configuredCount++;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[SlotTextureImporterHelper] Successfully configured {configuredCount} textures as Sprites.");
        }
    }
}
