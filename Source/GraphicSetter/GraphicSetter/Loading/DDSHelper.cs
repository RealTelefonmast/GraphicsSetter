using System.IO;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace GraphicSetter;

public class DDSHelper
{
    public static Texture2D TryLoadDDS(VirtualFile file)
    {
        var ddsPath = Path.ChangeExtension(file.FullPath, ".dds");
        if (!File.Exists(ddsPath))
            return null;

        var texture = DDSLoader.LoadDDS(ddsPath);
        if (!texture)
        {
            if (!DDSLoader.error.NullOrEmpty())
                Log.Warning($"DDS loading failed for {file.Name}: {DDSLoader.error}");
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(file.Name);
        return texture;
    }
}