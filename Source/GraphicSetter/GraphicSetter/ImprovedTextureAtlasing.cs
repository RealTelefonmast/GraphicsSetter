using System;
using UnityEngine;
using Verse;

namespace GraphicSetter
{
    public enum AntiAliasing : byte
    {
        None = 0,
        TwoX = 2,
        FourX = 4,
        EightX = 8,
    }
    
    public static class ImprovedTextureAtlasing
    {
        public static Texture2D MakeReadableTextureInstance_Patched(Texture2D source)
        {
            DeepProfiler.Start("MakeReadableTextureInstance");
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            temporary.name = "MakeReadableTexture_Temp";
            Graphics.Blit(source, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D texture2D = new Texture2D(source.width, source.height);
            texture2D.ReadPixels(new Rect(0f, 0f, (float)temporary.width, (float)temporary.height), 0, 0);
            
            //Apply Settings
            var settings = GraphicsSettings.mainSettings;

            texture2D.filterMode = settings.filterMode;
            texture2D.anisoLevel = settings.anisoLevel;
            texture2D.mipMapBias = settings.mipMapBias;
            
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            DeepProfiler.End();
            return texture2D;
        }
    }
}
