using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using static HarmonyLib.AccessTools;

namespace GraphicSetter
{
    [StaticConstructorOnStartup]
    internal static class StaticContent
    {
        //Inspired by Brrainz' patches
        internal static readonly AccessTools.FieldRef<Dialog_ModSettings, Mod> selModByRef = FieldRefAccess<Dialog_ModSettings, Mod>("mod");
        internal static readonly GraphicsDriver CoroutineDriver;
        internal static readonly MemoryData MemoryData;

        //
        static StaticContent()
        {
            MemoryData = new MemoryData();
            var routineHolder = new GameObject("GraphicsSettingsDriver");
            Object.DontDestroyOnLoad(routineHolder);
            routineHolder.AddComponent<GraphicsDriver>();
            CoroutineDriver = routineHolder.GetComponent<GraphicsDriver>();
        }

        //
        public static readonly Texture2D clear = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        public static readonly Texture2D grey = SolidColorMaterials.NewSolidColorTexture(Color.grey);
        public static readonly Texture2D blue = SolidColorMaterials.NewSolidColorTexture(new ColorInt(38, 169, 224).ToColor);
        public static readonly Texture2D yellow = SolidColorMaterials.NewSolidColorTexture(new ColorInt(249, 236, 49).ToColor);
        public static readonly Texture2D red = SolidColorMaterials.NewSolidColorTexture(new ColorInt(190, 30, 45).ToColor);
        public static readonly Texture2D green = SolidColorMaterials.NewSolidColorTexture(new ColorInt(41, 180, 115).ToColor);
        public static readonly Texture2D white = SolidColorMaterials.NewSolidColorTexture(new ColorInt(255, 255, 255).ToColor);
        public static readonly Texture2D black = SolidColorMaterials.NewSolidColorTexture(new ColorInt(15, 11, 12).ToColor);
    }
}
