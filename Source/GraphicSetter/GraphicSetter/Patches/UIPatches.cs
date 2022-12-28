using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GraphicSetter.Patches;

internal class UIPatches
{
    [HarmonyPatch(typeof(MainMenuDrawer))]
    [HarmonyPatch("DoMainMenuControls")]
    private static class DoMainMenuControlsPatch
    {
        public static float addedHeight = 45f + 7f;
        public static List<ListableOption> OptionList;
        private static MethodInfo ListingOption = SymbolExtensions.GetMethodInfo(() => AdjustList(null));

        static void AdjustList(List<ListableOption> optList)
        {
            var label = "Options".Translate();
            var idx = optList.FirstIndexOf(opt => opt.label == label);
            if (idx > 0 && idx < optList.Count)
                optList.Insert(idx + 1, new ListableOption("GS_MenuTitle".Translate(), delegate
                {
                    var dialog = new Dialog_ModSettings(GraphicSetter.ModRef);
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
            var m_DrawOptionListing =
                SymbolExtensions.GetMethodInfo(() => OptionListingUtility.DrawOptionListing(Rect.zero, null));

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
}