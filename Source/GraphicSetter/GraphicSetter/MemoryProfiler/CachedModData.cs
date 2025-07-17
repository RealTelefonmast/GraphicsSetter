using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GraphicSetter;

internal class CachedModData(ModContentPack mod)
{
    internal ModContentPack ModReference = mod;
    internal readonly Dictionary<string, int> TextureCountByAtlasType = new();
    internal long MemoryUsage;
    internal int TotalTextureCount;
    internal int TexturesInAtlasCount;
    internal int TexturesWithoutAtlasCount;

    public void RegisterTexture(Texture2D texture)
    {
        var textureSize = new Vector2(texture.width, texture.height);

        TotalTextureCount++;
        MemoryUsage += EstimateTextureMemorySize(texture);

        if (textureSize.x >= 512 || textureSize.y >= 512)
            TexturesWithoutAtlasCount++;
        else
            TexturesInAtlasCount++;
    }

    private static long EstimateTextureMemorySize(Texture2D texture)
    {
        if (texture == null) return 0;

        var bytesPerPixel = texture.format switch
        {
            TextureFormat.RGBA32 => 4,
            TextureFormat.ARGB32 => 4,
            TextureFormat.RGB24 => 3,
            TextureFormat.RGB565 => 2,
            TextureFormat.DXT1 => 0,
            TextureFormat.DXT5 => 0,
            _ => 0
        };
        
        if (bytesPerPixel == 0)
            switch (texture.format)
            {
                case TextureFormat.DXT1:
                    return (long)(texture.width * texture.height * 0.5f);
                case TextureFormat.DXT5:
                    return texture.width * texture.height;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        long baseSize = texture.width * texture.height * bytesPerPixel;

        // Account for mipmaps (adds ~33% more memory)
        if (texture.mipmapCount > 1) 
            baseSize = (long)(baseSize * 1.33333f);

        return baseSize;
    }
}