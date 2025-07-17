using System.IO;
using HarmonyLib;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace GraphicSetter.Patches;

internal static class TextureLoadingPatch
{
    [HarmonyPatch(typeof(ModContentLoader<Texture2D>), "LoadTexture", MethodType.Normal)]
    public static class LoadTexture_Patch
    {
        private static bool Prefix(VirtualFile file, ref Texture2D __result)
        {
            __result = CustomLoad(file);
            return false;
        }

        private static Texture2D CustomLoad(VirtualFile file)
        {
            if (!file.Exists)
                return null;

            var texture2D = DDSHelper.TryLoadDDS(file);
            var ddsLoaded = texture2D != null;
            byte[] array = null;
            if (!ddsLoaded)
            {
                array = file.ReadAllBytes();
                texture2D = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                texture2D.LoadImage(array);
            }

            if ((texture2D.width < 4 || texture2D.height < 4 || !Mathf.IsPowerOfTwo(texture2D.width) ||
                 !Mathf.IsPowerOfTwo(texture2D.height)) && Prefs.TextureCompression)
            {
                var num = StaticTextureAtlas.CalculateMaxMipmapsForDxtSupport(texture2D);
                if (Prefs.LogVerbose)
                    Log.Warning(string.Format(
                        "Texture {0} is being reloaded with reduced mipmap count (clamped to {1}) due to non-power-of-two dimensions: ({2}x{3}). This will be slower to load, and will look worse when zoomed out. Consider using a power-of-two texture size instead.",
                        file.Name, num, texture2D.width, texture2D.height));
                if (!UnityData.ComputeShadersSupported)
                {
                    var texture2D2 = new Texture2D(texture2D.width, texture2D.height, TextureFormat.Alpha8, num, false);
                    Object.DestroyImmediate(texture2D);
                    texture2D = texture2D2;
                    array ??= file.ReadAllBytes();
                    texture2D.LoadImage(array);
                }
            }

            var flag = texture2D.width % 4 == 0 && texture2D.height % 4 == 0;
            if (Prefs.TextureCompression && flag)
            {
                if (!UnityData.ComputeShadersSupported)
                {
                    texture2D.Compress(true);
                    texture2D.filterMode = FilterMode.Trilinear;
                    texture2D.anisoLevel = 2;
                    texture2D.Apply(true, true);
                }
                else
                {
                    texture2D.filterMode = FilterMode.Trilinear;
                    texture2D.anisoLevel = 2;
                    texture2D.Apply(true, true);
                    texture2D = StaticTextureAtlas.FastCompressDXT(texture2D, true);
                }
            }
            else
            {
                texture2D.filterMode = FilterMode.Trilinear;
                texture2D.anisoLevel = 2;
                texture2D.Apply(true, true);
            }

            texture2D.name = Path.GetFileNameWithoutExtension(file.Name);
            return texture2D;
        }
    }
}