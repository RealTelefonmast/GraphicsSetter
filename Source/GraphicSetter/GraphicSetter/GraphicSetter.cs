using HarmonyLib;
using UnityEngine;
using Verse;

namespace GraphicSetter;

public class GraphicSetter : Mod
{
    public static GraphicSetter ModRef { get; private set; }

    public static GraphicsSettings Settings { get; private set; }

    public GraphicSetter(ModContentPack content) : base(content)
    {
        ModRef = this;
        Log.Message("[1.6]Graphics Setter - Loaded");
        Settings = GetSettings<GraphicsSettings>();
        //profiler = new ResourceProfiler();
        var graphics = new Harmony("com.telefonmast.graphicssettings.rimworld.mod");

        //Maybe obsolete?
        //graphics.Patch(AccessTools.Constructor(typeof(PawnTextureAtlas)), transpiler: new(typeof(GraphicsPatches.PawnTextureAtlasCtorPatch).GetMethod(nameof(GraphicsPatches.PawnTextureAtlasCtorPatch.Transpiler)), Priority.First));
        graphics.PatchAll();
    }

    public override void WriteSettings()
    {
        Settings.Write();
        base.WriteSettings();
    }

    public override string SettingsCategory()
    {
        return "GS_MenuTitle".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }
}