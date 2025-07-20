using System;
using System.IO;
using GraphicSetter.Patches;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace GraphicSetter;

public class DDSHelper
{
    public static bool TryLoadDDS(VirtualFile file, ref bool hasMipMapsSet, ref Texture2D texture2D)
    {
        var ddsExtensionPath = Path.ChangeExtension(file.FullPath, ".dds");

        if (!File.Exists(ddsExtensionPath))
            return false;
        
        var loadedFromDds = false;
        try
        {
            texture2D = DDSLoader.LoadDDS(ddsExtensionPath, out hasMipMapsSet, true);
        }
        catch (Exception exception)
        {
            Log.Warning($"Caught exception while loading '{ddsExtensionPath}': {exception}");
        }

        if (!DDSLoader.error.NullOrEmpty())
            Log.Warning($"DDS loading failed for '{ddsExtensionPath}': {DDSLoader.error}");

        if (!texture2D)
        {
            Log.Warning($"Couldn't load .dds from '{ddsExtensionPath}'. Loading as png instead.");
        }
        else
        {
            loadedFromDds = true;
            if (!hasMipMapsSet)
                hasMipMapsSet = TextureLoadingPatch.CheckMipMapFix(texture2D, file);
        }

        return loadedFromDds;
    }
}