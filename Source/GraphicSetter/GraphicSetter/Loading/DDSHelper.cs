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
        texture2D = DDSLoader.LoadDDS(ddsExtensionPath, out hasMipMapsSet, true);

        if (!DDSLoader.error.NullOrEmpty())
            Log.Warning($"DDS loading failed for '{file.FullPath}': {DDSLoader.error}");

        if (!texture2D)
        {
            Log.Warning($"Couldn't load .dds from '{file.Name}'. Loading as png instead.");
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