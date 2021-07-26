using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld.IO;
using RuntimeAudioClipLoader;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Verse;

namespace GraphicSetter
{
    public class GraphicSetter : Mod
    {
        public static GraphicsSettings settings;

        public GraphicSetter(ModContentPack content) : base(content)
        {
            Log.Message("[1.3]Graphics Setter - Loaded");
            settings = GetSettings<GraphicsSettings>();
            //profiler = new ResourceProfiler();
            Harmony graphics = new Harmony("com.telefonmast.graphicssettings.rimworld.mod");
            graphics.PatchAll();
        }

        [HarmonyPatch(typeof(ModContentLoader<Texture2D>))]
        [HarmonyPatch("LoadTexture")]
        public static class LoadPNGPatch
        {
            static bool Prefix(VirtualFile file, ref Texture2D __result)
            {
                __result = StaticTools.LoadTexture(file);
                return false;
            }
        }

        [HarmonyPatch(typeof(MainMenuDrawer))]
        [HarmonyPatch("DoMainMenuControls")]
        public static class DoMainMenuControlsPatch
        {
            public static float addedHeight = 45f + 7f;
            public static List<ListableOption> OptionList;
            private static MethodInfo ListingOption = SymbolExtensions.GetMethodInfo(() => AdjustList(null));

            static void AdjustList(List<ListableOption> optList)
            {
                var label = "Options".Translate();
                var idx = optList.FirstIndexOf(opt => opt.label == label);
                if (idx > 0 && idx < optList.Count) optList.Insert(idx + 1, new ListableOption("Graphics Settings", delegate
                {
                    var dialog = new Dialog_ModSettings();
                    var me = LoadedModManager.GetMod<GraphicSetter>();
                    StaticContent.selModByRef(dialog) = me;
                    Find.WindowStack.Add(dialog);
                }, null));
                OptionList = optList;
            }

            static bool Prefix(ref Rect rect, bool anyMapFiles)
            {
                rect = new Rect(rect.x, rect.y, rect.width, rect.height + addedHeight);
                return true;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var m_DrawOptionListing = SymbolExtensions.GetMethodInfo(() => OptionListingUtility.DrawOptionListing(Rect.zero, null));

                var instructionsList = instructions.ToList();
                var patched = false;
                for (var i = 0; i < instructionsList.Count; i++)
                {
                    var instruction = instructionsList[i];
                    if (i + 2 < instructionsList.Count)
                    {
                        var checkingIns = instructionsList[i + 2];
                        if (!patched && checkingIns != null && checkingIns.Calls(m_DrawOptionListing))
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc_2);
                            yield return new CodeInstruction(OpCodes.Call, ListingOption);
                            patched = true;
                        }
                    }
                    yield return instruction;
                }
            }
        }
        

        public override void WriteSettings()
        {
            settings.Write();
            base.WriteSettings();
        }

        public override string SettingsCategory()
        {
            return "Graphics Setter";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }
    }

    public class SettingsGroup : IExposable
    {
        public int anisoLevel = 6;
        public FilterMode filterMode = FilterMode.Trilinear;
        public bool useMipMap = true;
        public float mipMapBias = -0.5f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref anisoLevel, "anisoLevel");
            Scribe_Values.Look(ref useMipMap, "useMipMap");
            Scribe_Values.Look(ref filterMode, "filterMode");
            Scribe_Values.Look(ref mipMapBias, "mipMapBias");
        }

        public bool IsDefault()
        {
            if (anisoLevel != 6) return false;
            if (filterMode != FilterMode.Trilinear) return false;
            if (useMipMap != true) return false;
            if (mipMapBias != -0.5f) return false;
            return true;
        }

        public void Reset()
        {
            anisoLevel = 6;
            filterMode = FilterMode.Trilinear;
            useMipMap = true;
            mipMapBias = -0.5f;
        }
    }

    public class GraphicsSettings : ModSettings
    {
        private SettingsGroup lastSettings = new SettingsGroup();
        public bool CausedMemOverflow = false;

        internal enum GraphicsTabOption
        {
            Advanced,
            Memory
        }

        //SETTINGS
        public SettingsGroup mainSettings;

        //FIXED RANGE DATA
        public readonly FloatRange anisoRange = new FloatRange(1, 9);
        public readonly FloatRange MipMapBiasRange = new FloatRange(-1f, 0.25f);

        [Obsolete] //Players should have own control over the calculation
        private bool DoFirstTime = false;

        private GraphicsTabOption SelTab { get; set; } = GraphicsTabOption.Advanced;

        public GraphicsSettings()
        {
            mainSettings = new SettingsGroup();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            //
            GUI.BeginGroup(inRect);
            Rect tabRect = new Rect(0, TabDrawer.TabHeight, inRect.width, 0);
            Rect menuRect = new Rect(0, TabDrawer.TabHeight, inRect.width, inRect.height - TabDrawer.TabHeight);

            Widgets.DrawMenuSection(menuRect);
            //
            var tabs = new List<TabRecord>();
            tabs.Add(new TabRecord("Advanced", delegate { SelTab = GraphicsTabOption.Advanced;}, SelTab == GraphicsTabOption.Advanced));
            tabs.Add(new TabRecord("Memory", delegate { SelTab = GraphicsTabOption.Memory; }, SelTab == GraphicsTabOption.Memory));
            TabDrawer.DrawTabs(tabRect, tabs);

            switch (SelTab)
            {
                case GraphicsTabOption.Advanced:
                    DrawAdvanced(menuRect.ContractedBy(15));
                    break;
                case GraphicsTabOption.Memory:
                    DrawMemory(menuRect.ContractedBy(10));
                    break;
            }

            GUI.EndGroup();
        }

        public bool AnySettingsChanged()
        {
            if (mainSettings.anisoLevel != lastSettings.anisoLevel)
                return true;
            if (mainSettings.filterMode != lastSettings.filterMode)
                return true;
            if (mainSettings.mipMapBias != lastSettings.mipMapBias)
                return true;
            if (mainSettings.useMipMap != lastSettings.useMipMap)
                return true;
            return false;
        }

        private void DrawAdvanced(Rect rect)
        {
            GUI.BeginGroup(rect);

            float curY = 0f;
            CheckBox("(Recommended) Activate Mip-Mapping", ref curY, ref mainSettings.useMipMap);
            Text.Anchor = TextAnchor.UpperCenter;
            if (mainSettings.useMipMap)
                mainSettings.mipMapBias = LabeledSlider("MipMap Bias", ref curY, MipMapBiasRange, mainSettings.mipMapBias, "Sharpest", "Blurriest", "This value changes how blurry textures can get depending on zoom, it is recommended to be equal or below 0.", 0.05f);
            curY += 10f;
            mainSettings.anisoLevel = (int)LabeledSlider("Anisotropic Filter Level", ref curY, anisoRange, (float)mainSettings.anisoLevel, "", "", "Set the level of anisotropic filtering, higher levels may reduce performance on older graphics cards.", 1);
            Text.Anchor = 0;
            curY += 10f;
            SetFilter(ref curY);
            curY += 10;

            Rect resetButton = new Rect(0, curY, 120, 25);
            if (!mainSettings.IsDefault())
            {
                if (Widgets.ButtonText(resetButton, "Reset"))
                {
                    mainSettings.Reset();
                }
            }

            if (AnySettingsChanged())
            {
                GUI.color = Color.red;
                Text.Font = GameFont.Medium;
                string text = "You will have to restart the game to apply changes!";
                Vector2 size = Text.CalcSize(text);
                float x2 = (rect.width - size.x) / 2f;
                float x3 = (rect.width - 150) / 2f;
                float y2 = rect.yMax - 150;
                Widgets.Label(new Rect(x2, y2, size.x, size.y), text);
                if (Widgets.ButtonText(new Rect(x3, y2 + size.y, 150, 45), "Restart Game", true, true))
                {
                    this.Write();
                    GenCommandLine.Restart();
                }
                //...
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }

            GUI.EndGroup();
        }

        public void DrawMemory(Rect rect)
        {
            StaticContent.MemoryData.DrawMemoryData(rect);
        }

        //TODO: Figure a way to use this
        private void ReloadTextures()
        {
            int i = 0,
                k = 0;
            foreach (ModContentPack mod in LoadedModManager.RunningMods)
            {
                i++;
                ModContentHolder<Texture2D> mch = Traverse.Create(mod).Field("textures").GetValue<ModContentHolder<Texture2D>>();
                Log.Message("Got modcontentholder for " + mod.PackageId + " with " + mch.contentList.Count + " textures.");
                mod.assemblies.ReloadAll();
                mch.ClearDestroy();
                mch.ReloadAll();
                mod.AllDefs.Do<Def>(delegate (Def def)
                {
                    if(def is ThingDef thingDef)
                    {
                        if(thingDef.graphic != null)
                            thingDef.graphic = null;
                        if (thingDef.graphicData != null)
                        {
                            Traverse.Create(thingDef.graphicData).Field("cachedGraphic").SetValue(null);
                            thingDef.graphic = thingDef.graphicData.Graphic;
                        }
                    }
                });
                k += mch.contentList.Count;
            }
            Log.Message("Reloaded " + k + " textures for: " + i + " mods");
        }

        public void CheckBox(string label, ref float curY, ref bool flag)
        {
            Vector2 size = Text.CalcSize(label);
            Rect rect = new Rect(0, curY, size.x + 25, size.y);
            Widgets.CheckboxLabeled(rect, label, ref flag);
            curY = rect.yMax;
        }

        public float LabeledSlider(string label, ref float curY, FloatRange range, float value, string leftLabel, string rightLabel, string tooltip = null, float roundTo = 0.1f)
        {
            float val = 0;
            Vector2 size = Text.CalcSize(label);
            Rect toolTipRect = new Rect(0, curY, 200f, size.y *2);
            Widgets.Label(toolTipRect.TopHalf(), label);
            val = Widgets.HorizontalSlider(toolTipRect.BottomHalf(), value, range.min, range.max, false, value.ToString(), leftLabel, rightLabel, roundTo);
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(toolTipRect, tooltip);
            }
            curY = toolTipRect.yMax;
            return val;
        }

        public void TwoEndedLabel(string label1, string label2, float width, ref float curY)
        {
            Text.Font = GameFont.Tiny;
            Vector2 size1 = Text.CalcSize(label1);
            Vector2 size2 = Text.CalcSize(label2);
            Rect left = new Rect(new Vector2(0, curY),size1);
            Rect right = new Rect(new Vector2(width-size2.x, curY),size2);
            Widgets.Label(left,label1);
            Widgets.Label(right, label2);
            curY += size1.y;
            Text.Font = GameFont.Small;
        }

        public void SetFilter(ref float curY)
        {
            Widgets.Label(new Rect(0, curY, 200, 25f), "Texture Filtering: ");
            Rect button1 = new Rect(20, curY + 25f, 85, 22f);
            Rect button2 = new Rect(button1.x, button1.yMax, button1.width, button1.height);
            if (Widgets.RadioButtonLabeled(button1,FilterMode.Bilinear.ToString(), mainSettings.filterMode == FilterMode.Bilinear))
            {
                mainSettings.filterMode = FilterMode.Bilinear;
            }
            if (Widgets.RadioButtonLabeled(button2, FilterMode.Trilinear.ToString(), mainSettings.filterMode == FilterMode.Trilinear))
            {
                mainSettings.filterMode = FilterMode.Trilinear;
            }

            curY = button2.yMax;
        }

        public override void ExposeData()
        {         
            base.ExposeData();
            Scribe_Deep.Look(ref mainSettings, "settings");
            Scribe_Values.Look(ref CausedMemOverflow, "causedOverflow");

            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                mainSettings ??= new SettingsGroup();
                lastSettings = new SettingsGroup();
                lastSettings.anisoLevel = mainSettings.anisoLevel;
                lastSettings.useMipMap = mainSettings.useMipMap;
                lastSettings.filterMode = mainSettings.filterMode;
                lastSettings.mipMapBias = mainSettings.mipMapBias;
            }
        }
    }
}
