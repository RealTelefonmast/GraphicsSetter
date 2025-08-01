﻿using System;
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
        if (!texture)
            return;
        
        try
        {
            var textureSize = new Vector2(texture.width, texture.height);

            TotalTextureCount++;
            MemoryUsage += EstimateTextureMemorySize(texture);

            if (textureSize.x >= 512 || textureSize.y >= 512)
                TexturesWithoutAtlasCount++;
            else
                TexturesInAtlasCount++;
        }
        catch (Exception ex)
        {
            Log.Error($"Exception registering texture for memory calculation:{ex}");
        }
    }

    private static long EstimateTextureMemorySize(Texture2D texture)
    {
        if (texture == null) return 0;

        var textureFormat = texture.format;
        var bytesPerPixel = textureFormat switch
        {
            TextureFormat.RGBA32 => 4,
            TextureFormat.ARGB32 => 4,
            TextureFormat.RGB24 => 3,
            TextureFormat.RGB565 => 2,
            TextureFormat.DXT1 => 0,
            TextureFormat.DXT5 or TextureFormat.BC7 => 0,
            _ => 0
        };
        
        if (bytesPerPixel == 0)
        {
            return textureFormat switch
            {
                TextureFormat.DXT1 => (long)(texture.width * texture.height * 0.5f),
                TextureFormat.DXT5 or TextureFormat.BC7 => texture.width * texture.height,
                _ => throw new ArgumentOutOfRangeException($"Unknown texture format: {textureFormat}")
            };
        }

        long baseSize = texture.width * texture.height * bytesPerPixel;

        // Account for mipmaps (adds ~33% more memory)
        if (texture.mipmapCount > 1) 
            baseSize = (long)(baseSize * 1.33333f);

        return baseSize;
    }
}