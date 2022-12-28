using HarmonyLib;
using UnityEngine;
using Verse;

namespace GraphicSetter.Patches;

internal class TextureAtlasPatches
{
    
    [HarmonyPatch(typeof(TextureAtlasHelper), nameof(TextureAtlasHelper.MakeReadableTextureInstance))]
    private static class MakeReadableTextureInstance_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Texture2D source, ref Texture2D __result)
        {
            __result = ImprovedTextureAtlasing.MakeReadableTextureInstance_Patched(source);
            return false;
        }
    }
    
    /*
    [HarmonyPatch(typeof(StaticTextureAtlas), nameof(StaticTextureAtlas.Bake))]
    private static class StaticTextureAtlas_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(StaticTextureAtlas __instance)
        {
            var texture2D = __instance.colorTexture;
            
            //Apply Settings
            var settings = GraphicsSettings.mainSettings;

            texture2D.filterMode = settings.filterMode;
            texture2D.anisoLevel = settings.anisoLevel;
            texture2D.mipMapBias = settings.mipMapBias;

            //
            __instance.colorTexture.Apply(true, true);
        }
    }
    */
}