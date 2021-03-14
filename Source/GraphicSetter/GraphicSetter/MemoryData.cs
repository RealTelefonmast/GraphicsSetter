using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace GraphicSetter
{
    public class MemoryData
    {
        //private static Dictionary<ModContentPack, long> RawMemoryUsageByMod = new Dictionary<ModContentPack, long>();
        private static readonly Dictionary<ModContentPack, long> MemoryUsageByMod = new Dictionary<ModContentPack, long>();

        private static IEnumerable<ModContentPack> CurrentMods => LoadedModManager.RunningMods.Where(t => t.GetContentHolder<Texture2D>().contentList.Any());
        private bool shouldRecalc = false;
        private bool shouldStop = false;

        private long TotalBytes => MemoryUsageByMod.Sum(t => t.Value);

        private long TotalRAM => SystemInfo.systemMemorySize * 1000000L;
        private long TotalVRAM => SystemInfo.graphicsMemorySize * 1000000L;
        private float TotalPctUsage => (float)(TotalBytes / (double)TotalVRAM);

        private bool Calculating => shouldRecalc;

        private static readonly float CriticalPct = 0.8f;//0.75f;

        private bool Critical => TotalPctUsage > CriticalPct;

        public bool MEMOVERFLOW => TotalPctUsage > 1f;


        public void Notify_SettingsChanged()
        {
            shouldRecalc = false;
            shouldStop = false;
            MemoryUsageByMod.Clear();
        }

        public Coroutine routine;

        public void Notify_ChangeState()
        {
            if (shouldRecalc)
            {
                shouldStop = !shouldStop;
                return;
            }
            routine = StaticContent.CoroutineDriver.StartCoroutine(DoTheThing());
            shouldRecalc = true;
        }

        public IEnumerator DoTheThing()
        {
            MemoryUsageByMod.Clear();
            int count = CurrentMods.Count();
            int k = 0;
            while (shouldStop || k < count)
            {
                if (shouldStop)
                {
                    yield return null;
                    continue;
                }
                var mod = CurrentMods.ElementAt(k);
                MemoryUsageByMod.Add(mod, 0);
                Dictionary<string, FileInfo> allFilesForMod = ModContentPack.GetAllFilesForMod(mod, GenFilePaths.ContentPath<Texture2D>(), (ModContentLoader<Texture2D>.IsAcceptableExtension));
                int i = 0;
                while (shouldStop || i < allFilesForMod.Count)
                {
                    if (shouldStop)
                    {
                        yield return null;
                        continue;
                    }
                    var pair = allFilesForMod.ElementAt(i);
                    MemoryUsageByMod[mod] += StaticTools.TextureSize(new VirtualFileWrapper(pair.Value));
                    i++;
                    if (i % 3 == 0) yield return null;
                }
                k++;
            }
            shouldRecalc = false;
            GraphicSetter.settings.CausedMemOverflow = MEMOVERFLOW;
        }

        private float MemoryPctOf(ModContentPack mod, out long memUsage)
        {
            memUsage = 0;
            if (!MemoryUsageByMod.ContainsKey(mod)) return 0;
            memUsage = MemoryUsageByMod[mod];
            return (float)((double)memUsage / (double)TotalBytes);
        }

        private string MemoryString(long memUsage, bool cap = false)
        {
            //return memUsage + " bytes";
            if (cap && memUsage > TotalVRAM)
            {
                return ">" + MemoryString(TotalVRAM);
            }
            if (memUsage < 1000)
            {
                return memUsage + " bytes";
            }

            if (memUsage < 1000000)
            {
                return Math.Round((memUsage / 1000d), 2) + "kB";
            }

            if (memUsage < 1000000000)
            {
                return Math.Round((memUsage / 1000000d), 2) + "MB";
            }
            return Math.Round((memUsage / 1000000000d), 2) + "GB";
        }

        //Render Data
        private Vector2 scrollview = new Vector2(0,0);

        public void DrawMemoryData(Rect rect)
        {
            Rect topHalf = rect.TopPart(0.7f);
            Rect bottomHalf = rect.BottomPart(0.3f);
            DrawModList(topHalf);
            WriteProcessingData(bottomHalf);
        }

        public void DrawModList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect newRect = rect.ContractedBy(5);
            GUI.BeginGroup(newRect);
            int y = 0;
            Rect viewRect = new Rect(0, 0, newRect.width, CurrentMods.Count() * 20);
            Widgets.BeginScrollView(new Rect(0, 0, newRect.width, newRect.height), ref scrollview, viewRect, false);
            var list = MemoryUsageByMod.ToList();
            list.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
            foreach (var mod in list)
            {
                var pct = MemoryPctOf(mod.Key, out long memUsage);
                var text = mod.Key.Name + " (" + MemoryString(memUsage) + ") " + pct.ToStringPercent();
                var tipRect = new Rect(0, y, rect.width, 20);
                WidgetRow row = new WidgetRow(0, y, UIDirection.RightThenDown);
                row.FillableBar(newRect.width, 18, pct, text, StaticContent.blue, StaticContent.clear);
                Widgets.DrawHighlightIfMouseover(tipRect);
                TooltipHandler.TipRegion(tipRect, text);
                y += 20;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        public void WriteProcessingData(Rect rect)
        {
            GUI.BeginGroup(rect);

            Rect buttonRect = new Rect(0, 5, rect.width * 0.2f, 22);
            Rect barRect = new Rect(buttonRect.xMax + 5, 5, rect.width - buttonRect.width - 5, 22);
            float curY = buttonRect.height;
            string text = shouldRecalc ? (shouldStop ? "Continue" : "Stop" ) : "Recalc";
            if (Widgets.ButtonText(buttonRect, text, true, false))
            {
                Notify_ChangeState();
            }
            Widgets.FillableBar(barRect, TotalPctUsage, StaticContent.blue, Texture2D.blackTexture, true);
            Text.Anchor = TextAnchor.MiddleCenter;
            string label = MEMOVERFLOW ? "Not Enough VRAM" : MemoryString(TotalBytes) + "/" + MemoryString(TotalVRAM);
            Widgets.Label(barRect, label);
            Text.Anchor = default;

            if (Calculating)
            {
                Rect textRect = new Rect(0, curY + 5, rect.width, 18);
                Widgets.Label(textRect, "Calculating... Please wait. (" + MemoryUsageByMod.Count + "/" + CurrentMods.Count() + ")");
                curY = textRect.yMax;
            }
            else if (GraphicSetter.settings.AnySettingsChanged())
            {
                string settingsChangedLabel = "Settings changed, recalculate to check the memory use!";
                var textSize = Text.CalcHeight(settingsChangedLabel, rect.width);
                Rect textRect = new Rect(0, curY + 5, rect.width, textSize);
                Widgets.Label(textRect, settingsChangedLabel);
                curY = textRect.yMax;
            }

            if (Critical)
            {
                Text.Font = GameFont.Small;
                string warningLabel = "Warning:\n" + (MEMOVERFLOW ? "Your system cannot support these settings.\nCompression will be enabled on startup." : "Your system may struggle with these settings.");
                var textSize = Text.CalcSize(warningLabel);
                Rect warningLabelRect = new Rect(0, curY + 5, textSize.x, textSize.y);
                Widgets.Label(warningLabelRect, warningLabel);
                Text.Font = default;
            }
            GUI.EndGroup();
        }
    }
}
