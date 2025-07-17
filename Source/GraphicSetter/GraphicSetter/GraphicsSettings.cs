using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GraphicSetter;

public class SettingsGroup : IExposable
{
    public bool enableDDSLoading = true;
    public bool overrideMipMapBias = false;
    public float mipMapBias = 0.0f;
    public bool verboseLogging = false;
    
    public static readonly FloatRange MipMapBiasRange = new FloatRange(-1f, 1f);
    
    public void ExposeData()
    {
        Scribe_Values.Look(ref enableDDSLoading, "enableDDSLoading", true);
        Scribe_Values.Look(ref overrideMipMapBias, "overrideMipMapBias", false);
        Scribe_Values.Look(ref mipMapBias, "mipMapBias", 0.0f);
        Scribe_Values.Look(ref verboseLogging, "verboseLogging", false);
    }
    
    public bool IsDefault()
    {
        return enableDDSLoading && !overrideMipMapBias && mipMapBias == 0.0f && !verboseLogging;
    }
    
    public void Reset()
    {
        enableDDSLoading = true;
        overrideMipMapBias = false;
        mipMapBias = 0.0f;
        verboseLogging = false;
    }
}

public class GraphicsSettings : ModSettings
{
    public static SettingsGroup mainSettings = new SettingsGroup();
    
    internal enum GraphicsTabOption
    {
        Advanced,
        Memory
    }
    
    private GraphicsTabOption SelTab { get; set; } = GraphicsTabOption.Advanced;
    
    public GraphicsSettings()
    {
        mainSettings = new SettingsGroup();
    }
    
    public void DoSettingsWindowContents(Rect inRect)
    {
        GUI.BeginGroup(inRect);
        
        // Tab area
        Rect tabRect = new Rect(0, TabDrawer.TabHeight, inRect.width, 0);
        Rect menuRect = new Rect(0, TabDrawer.TabHeight, inRect.width, inRect.height - TabDrawer.TabHeight);

        // Draw background
        Widgets.DrawMenuSection(menuRect);
        
        // Create tabs
        var tabs = new List<TabRecord>();
        tabs.Add(new TabRecord("GS_AdvancedTab".Translate(), delegate { SelTab = GraphicsTabOption.Advanced; }, SelTab == GraphicsTabOption.Advanced));
        tabs.Add(new TabRecord("GS_MemoryTab".Translate(), delegate { SelTab = GraphicsTabOption.Memory; }, SelTab == GraphicsTabOption.Memory));
        TabDrawer.DrawTabs(tabRect, tabs);
        
        // Content area with padding
        var contentRect = menuRect.ContractedBy(15);
        
        switch (SelTab)
        {
            case GraphicsTabOption.Advanced:
                DrawAdvanced(contentRect);
                break;
            case GraphicsTabOption.Memory:
                DrawMemory(contentRect);
                break;
        }
        
        GUI.EndGroup();
    }
    
    private void DrawAdvanced(Rect rect)
    {
        var listing = new Listing_Standard();
        listing.Begin(rect);
        
        // Main header with icon/symbol
        //DrawSectionHeader(listing, "DDS Texture Loading", "⚡");
        
        // Main toggle with better spacing
        //listing.Gap(8);
        var enableRect = listing.GetRect(26);
        bool wasEnabled = mainSettings.enableDDSLoading;
        Widgets.CheckboxLabeled(enableRect, "Enable DDS texture loading", ref mainSettings.enableDDSLoading);
        
        // Subtle description
        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        Text.Font = GameFont.Tiny;
        var ddsLoadingDesc = "Loads compressed textures when available\n • Reduces memory usage\n • Improves loading times";
        var size = Text.CalcSize(ddsLoadingDesc);
        var descRect = listing.GetRect(size.y);
        Widgets.Label(descRect.ContractedBy(25, 0), 
            ddsLoadingDesc);
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        
        //listing.Gap(25);
        
        // Advanced section with visual separator
        //DrawSectionHeader(listing, "Advanced Options", "⚙");
        //listing.Gap(8);
        
        /*// Mipmap bias section
        var biasRect = listing.GetRect(24);
        Widgets.CheckboxLabeled(biasRect, "Override Mipmap Bias", ref mainSettings.overrideMipMapBias);
        
        if (mainSettings.overrideMipMapBias)
        {
            listing.Gap(10);
            
            // Custom styled slider
            var sliderBg = listing.GetRect(30);
            
            // Draw slider background
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            Widgets.DrawBox(sliderBg);
            GUI.color = Color.white;
            
            // Draw the slider
            var sliderInner = sliderBg.ContractedBy(3);
            mainSettings.mipMapBias = Widgets.HorizontalSlider(
                sliderInner,
                mainSettings.mipMapBias,
                SettingsGroup.MipMapBiasRange.min,
                SettingsGroup.MipMapBiasRange.max,
                true,
                $"Bias: {mainSettings.mipMapBias:F2}",
                "Blurry",
                "Sharp",
                0.01f);
            
            // Visual indicator below
            listing.Gap(4);
            GUI.color = GetMipmapColor(mainSettings.mipMapBias);
            Text.Anchor = TextAnchor.MiddleCenter;
            var indicatorRect = listing.GetRect(18);
            Widgets.Label(indicatorRect, GetMipmapDescription(mainSettings.mipMapBias));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }*/
        
        // Bottom section with stats
        listing.Gap(30);
        DrawQuickStats(listing);
        
        // Reset button if not default
        if (!mainSettings.IsDefault())
        {
            listing.Gap(20);
            var buttonRect = listing.GetRect(35);
            var centeredButton = new Rect(buttonRect.center.x - 100, buttonRect.y, 200, 35);
            
            if (Widgets.ButtonText(centeredButton, "Reset to Defaults", true, true, true))
            {
                mainSettings.Reset();
            }
        }
        
        listing.End();
    }
    
    private void DrawSectionHeader(Listing_Standard listing, string text, string icon = null)
    {
        var headerRect = listing.GetRect(30);
        
        // Draw separator line above
        var lineRect = new Rect(headerRect.x, headerRect.y + 5, headerRect.width, 1);
        GUI.color = new Color(0.3f, 0.3f, 0.3f);
        Widgets.DrawLineHorizontal(lineRect.x, lineRect.y, lineRect.width);
        GUI.color = Color.white;
        
        // Draw header text with icon
        Text.Font = GameFont.Medium;
        var headerTextRect = headerRect;
        headerTextRect.y += 10;
        
        if (!string.IsNullOrEmpty(icon))
        {
            GUI.color = new Color(0.8f, 0.8f, 0.3f); // Golden accent color
            var iconRect = new Rect(headerTextRect.x, headerTextRect.y, 30, 30);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(iconRect, icon);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            headerTextRect.x += 35;
        }
        
        Widgets.Label(headerTextRect, text);
        Text.Font = GameFont.Small;
        
        listing.Gap(5);
    }
    
    private void DrawQuickStats(Listing_Standard listing)
    {
        // Quick stats box
        var statsRect = listing.GetRect(60);
        
        GUI.color = new Color(0.15f, 0.15f, 0.15f);
        Widgets.DrawBox(statsRect);
        GUI.color = Color.white;
        
        var innerStats = statsRect.ContractedBy(10);
        
        // Calculate some basic stats
        int textureCount = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
        bool ddsActive = mainSettings.enableDDSLoading;
        
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = new Color(0.8f, 0.8f, 0.8f);
        
        var line1 = new Rect(innerStats.x, innerStats.y + 5, innerStats.width, 20);
        var line2 = new Rect(innerStats.x, innerStats.y + 25, innerStats.width, 20);
        
        Widgets.Label(line1, $"Status: {(ddsActive ? "DDS Loading Active" : "Standard Loading")}");
        Widgets.Label(line2, $"Textures in memory: {textureCount}");
        
        if (ddsActive)
        {
            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            var statusDot = new Rect(line1.xMax - 20, line1.y + 5, 10, 10);
            Widgets.DrawBoxSolid(statusDot, GUI.color);
        }
        
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }
    
    private Color GetMipmapColor(float bias)
    {
        if (bias < -0.5f) return new Color(0.4f, 0.6f, 1f); // Blue for performance
        if (bias < 0f) return new Color(0.4f, 0.8f, 0.8f);
        if (bias == 0f) return new Color(0.7f, 0.7f, 0.7f); // Gray for balanced
        if (bias < 0.5f) return new Color(0.8f, 0.8f, 0.4f);
        return new Color(1f, 0.6f, 0.4f); // Orange for quality
    }
    
    private string GetMipmapDescription(float bias)
    {
        if (bias < -0.5f) return "Performance mode";
        if (bias < 0f) return "Balanced-Performance";
        if (bias == 0f) return "Balanced (Default)";
        if (bias < 0.5f) return "Balanced-Quality";
        return "Quality mode";
    }
    
    public void DrawMemory(Rect rect)
    {
        // Your existing memory tab implementation
        StaticContent.MemoryData.DrawMemoryData(rect);
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref mainSettings, "settings");
        
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            mainSettings ??= new SettingsGroup();
        }
    }
}
