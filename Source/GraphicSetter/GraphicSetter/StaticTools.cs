using System;
using System.IO;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace GraphicSetter
{
    public static class StaticTools
    {
        //
        public static Texture2D LoadTexture(VirtualFile file, bool readable = false)
        {
            Texture2D texture2D = null;
            var settings = GraphicSetter.settings;
            try
            {
                string ddsExtensionPath = Path.ChangeExtension(file.FullPath, ".dds");
                if (File.Exists(ddsExtensionPath))
                {
                    texture2D = DDSLoader.LoadDDS(ddsExtensionPath);
                    if(!DDSLoader.error.NullOrEmpty())
                        Log.Warning("DDS Error: " + DDSLoader.error);
                    if(texture2D == null)
                        Log.Warning("Couldn't load .dds from " + file.Name + " loading as png instead.");
                }
                if (texture2D == null && file.Exists)
                {
                    byte[] data = file.ReadAllBytes();
                    texture2D = new Texture2D(2, 2, TextureFormat.Alpha8, settings.useMipMap);
                    texture2D.LoadImage(data);
                }

                if (texture2D == null)
                {
                    throw new Exception("Could not load texture at " + file.FullPath);
                }

                texture2D.Compress(true);
                texture2D.name = Path.GetFileNameWithoutExtension(file.Name);
                texture2D.filterMode = settings.filterMode;
                texture2D.anisoLevel = settings.anisoLevel;
                texture2D.mipMapBias = settings.mipMapBias;
                texture2D.Apply(true, !readable);
            }
            catch (Exception exception)
            {
                Log.Error("[Graphics Settings][" + (file?.Name ?? "Missing File...") + "] " + exception);
            }
            return texture2D;
        }
    

        public static long TextureSize(VirtualFile file)
        {
            var texture2D = LoadTexture(file, true);
            if (texture2D == null) return 0;

            long size = texture2D.GetRawTextureData().Length;
            new DisposableTexture(texture2D).Dispose();
            return size;
        }
    }
}
