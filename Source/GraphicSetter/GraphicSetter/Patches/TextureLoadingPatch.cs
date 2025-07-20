using System;
using System.IO;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace GraphicSetter.Patches;

internal static class TextureLoadingPatch
{
    [HarmonyPatch(typeof(ModContentLoader<Texture2D>), "LoadTexture", MethodType.Normal)]
    public static class LoadTexture_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(VirtualFile file, ref Texture2D __result)
        {
            __result = CustomLoad(file);
            return false;
        }

        public static Texture2D CustomLoad(VirtualFile file, bool readable = false)
        {
            Texture2D texture2D = null;
            var settings = GraphicsSettings.mainSettings;
            try
            {
                var hasMipMapsSet = false;
                var loadedFromDds = DDSHelper.TryLoadDDS(file, ref hasMipMapsSet, ref texture2D);

                if (!texture2D && file.Exists)
                {
                    var data = file.ReadAllBytes();
                    texture2D = new(2, 2, TextureFormat.Alpha8, true /*settings.useMipMap*/);
                    texture2D.LoadImage(data);
                    hasMipMapsSet = FixMipMapsIfNeeded(ref texture2D, data, file);
                }

                if (!texture2D)
                    throw new($"Could not load texture at '{file.FullPath}'.");

                if (!loadedFromDds && Prefs.TextureCompression)
                    texture2D.Compress(true);

                texture2D.name = Path.GetFileNameWithoutExtension(file.Name);
                texture2D.filterMode = FilterMode.Trilinear;
                
                texture2D.anisoLevel = 1;
                // 2 or higher is impossible to display with rimworld's orthographic camera.
                // Planets are loaded from asset bundles
                
                texture2D.mipMapBias = settings.mipMapBias;
                texture2D.Apply(!hasMipMapsSet, !readable);
            }
            catch (Exception exception)
            {
                Log.Error($"[Graphics Settings][{(file?.Name ?? "Missing File...")}] {exception}");
            }

            return texture2D;
        }
    }

    private static bool FixMipMapsIfNeeded(ref Texture2D texture2D, byte[] data, VirtualFile file)
    {
        if (!CheckMipMapFix(texture2D, file))
            return false;

        UnityEngine.Object.DestroyImmediate(texture2D);

        texture2D = new(2, 2, TextureFormat.Alpha8, false);
        texture2D.LoadImage(data);
        return true;
    }

    public static bool CheckMipMapFix(Texture2D texture2D, VirtualFile file)
    {
        var needsFix = NeedsMipMapFix(texture2D);
        if (needsFix)
            LogMipMapWarning(texture2D, file);

        return needsFix;
    }

    private static void LogMipMapWarning(Texture2D texture2D, VirtualFile file)
    {
        if (!Prefs.LogVerbose)
            return;

        Log.Warning($"Texture does not support mipmapping, needs to be divisible by 4 ({
            texture2D.width}x{texture2D.height}) for '{file.Name}'");
    }

    private static bool NeedsMipMapFix(Texture2D texture2D)
        => /*GraphicsSettings.mainSettings.useMipMap
            &&*/ (((texture2D.width & 3) != 0) | ((texture2D.height & 3) != 0));
}