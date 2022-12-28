using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GraphicSetter;

internal class CachedModData
{
    internal ModContentPack ModReference;
    internal readonly Dictionary<string, int> TextureCountByAtlasType = new();
    internal long MemoryUsage;
    internal int TotalTextureCount;
    internal int TexturesInAtlasCount;
    internal int TexturesWithoutAtlasCount;

    public CachedModData(ModContentPack mod)
    {
        ModReference = mod;
    }

    public void RegisterTexture(VirtualFileWrapper virtualFileWrapper)
    {
        TotalTextureCount++;
        MemoryUsage += StaticTools.TextureSize(virtualFileWrapper, out var textureSize);

        if (textureSize.x >= 512 || textureSize.y >= 512)
        {
            TexturesWithoutAtlasCount++;
        }
        else
        {
            TexturesInAtlasCount++;
        }
    }
}