#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;


namespace VFolders
{
    public class VFoldersPalette : ScriptableObject
    {
        public List<Color> colors = new();

        public bool colorsEnabled;

        public float colorSaturation = 1;
        public float colorBrightness = 1;
        public bool colorGradientsEnabled = true;

        public void ResetColors()
        {
            colors.Clear();

            for (int i = 0; i < colorsCount; i++)
                colors.Add(GetDefaultColor(i));

            colorsEnabled = true;

            this.Dirty();

        }


        public static Color GetDefaultColor(int colorIndex)
        {
            Color color = default;

            void grey()
            {
                if (colorIndex >= greyColorsCount) return;

#if UNITY_2022_1_OR_NEWER
                color = Greyscale(EditorGUIUtility.isProSkin ? .16f : .9f);
#else
                color = Greyscale(EditorGUIUtility.isProSkin ? .315f : .9f);
#endif

            }
            void rainbowDarkTheme()
            {
                if (colorIndex < greyColorsCount) return;
                if (!isDarkTheme) return;


                var t = (colorIndex - greyColorsCount.ToFloat()) / rainbowColorsCount;

                if (colorIndex == 0)
                    t += .01f;

                if (colorIndex == 1)
                    t -= .02f;

                if (colorIndex == 2)
                    t -= .015f;

                if (colorIndex == 3)
                    t -= .01f;

                if (colorIndex == 4)
                    t += .02f;

                if (colorIndex == 5)
                    t += .01f;


                if (colorIndex == 8)
                    t -= .01f;


                // color = HSLToRGB(t, .61f, .57f);
                color = ColorUtils.HSLToRGB(t, .61f, .57f);


                if (colorIndex == 0)
                    color *= 1.16f;

                if (colorIndex == 1)
                    color *= 1.17f;

                if (colorIndex == 2)
                    color *= 1.03f;

                if (colorIndex == 6)
                    color *= 1.2f;

                if (colorIndex == 7)
                    color *= 1.55f;

                if (colorIndex == 8)
                    color *= 1.2f;

                if (colorIndex == 9)
                    color *= 1.08f;


                color.a = .1f;

            }
            void rainbowLightTheme()
            {
                if (colorIndex < greyColorsCount) return;
                if (isDarkTheme) return;

                color = ColorUtils.HSLToRGB((colorIndex - greyColorsCount.ToFloat()) / rainbowColorsCount, .99f, .75f);

                if (colorIndex == 0)
                    color *= 1.1f;

                if (colorIndex == 1)
                    color *= 1.05f;

                color.a = .1f;

            }

            grey();
            rainbowDarkTheme();
            rainbowLightTheme();

            return color;

        }

        public static int greyColorsCount = 0;
        public static int rainbowColorsCount = 10;
        public static int colorsCount => greyColorsCount + rainbowColorsCount;




        public List<IconRow> iconRows = new();

        [System.Serializable]
        public class IconRow
        {
            public List<string> builtinIcons = new(); // names
            public List<string> customIcons = new(); // guids

            public bool enabled = true;

            public bool isCustom => !builtinIcons.Any() || customIcons.Any();
            public bool isEmpty => !builtinIcons.Any() && !customIcons.Any();
            public int iconCount => builtinIcons.Count + customIcons.Count;

            public IconRow(string[] builtinIcons) => this.builtinIcons = builtinIcons.ToList();
            public IconRow() { }

        }

        public void ResetIcons()
        {
            iconRows.Clear();

            iconRows.Add(new IconRow(new[]
            {
                "SceneAsset Icon",
                "Prefab Icon",
                "PrefabModel Icon",
                "Material Icon",
                "Texture Icon",
                "Mesh Icon",
                "cs Script Icon",
                "Shader Icon",
                "ComputeShader Icon",
                "ScriptableObject Icon",

            }));
            iconRows.Add(new IconRow(new[]
            {
                "Light Icon",
                "LightProbes Icon",
                "LightmapParameters Icon",
                "LightingDataAsset Icon",
                "Cubemap Icon"

            }));
            iconRows.Add(new IconRow(new[]
            {
            #if UNITY_6000_0_OR_NEWER
                "PhysicsMaterial Icon",
            #else
                "PhysicMaterial Icon",
            #endif
                "BoxCollider Icon",
                "TerrainCollider Icon",
                "MeshCollider Icon",
                "WheelCollider Icon",
                "Rigidbody Icon",

            }));
            iconRows.Add(new IconRow(new[]
            {
                "AudioClip Icon",
                "AudioMixerController Icon",
                "AudioMixerGroup Icon",
                "AudioEchoFilter Icon",
                "AudioSource Icon",

            }));
            iconRows.Add(new IconRow(new[]
            {
                "TextAsset Icon",
                "AssemblyDefinitionAsset Icon",
                "TerrainData Icon",
                "Terrain Icon",
                "AnimatorController Icon",
                "AnimationClip Icon",
                "Font Icon",
                "RawImage Icon",
                "Settings Icon",

            }));

            this.Dirty();

        }




        [ContextMenu("Export palette")]
        public void Export()
        {
            var packagePath = EditorUtility.SaveFilePanel("Export vFolders Palette", "", this.GetPath().GetFilename(withExtension: false), "unitypackage");

            var iconPaths = iconRows.SelectMany(r => r.customIcons).Select(r => r.ToPath()).Where(r => !r.IsNullOrEmpty());

            AssetDatabase.ExportPackage(iconPaths.Append(this.GetPath()).ToArray(), packagePath);

            EditorUtility.RevealInFinder(packagePath);

        }




        void Reset()
        {
            ResetColors();
            ResetIcons();

            VFolders.folderInfoCache.Clear();

        }

    }
}
#endif