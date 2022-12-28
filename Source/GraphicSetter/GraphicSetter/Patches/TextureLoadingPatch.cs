using HarmonyLib;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace GraphicSetter.Patches;

internal static class TextureLoadingPatch
{
    [HarmonyPatch(typeof(ModContentLoader<Texture2D>))]
    [HarmonyPatch("LoadTexture")]
    private static class LoadPNGPatch
    {
        static bool Prefix(VirtualFile file, ref Texture2D __result)
        {
            __result = StaticTools.LoadTexture(file);
            return false;
        }
    }
}