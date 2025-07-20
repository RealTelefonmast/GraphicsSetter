using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GraphicSetter;

// DDS importing based on // https://github.com/sarbian/DDSLoader/blob/master/DatabaseLoaderTexture_DDS.cs
public static class DDSLoader
{
    private const uint DDSD_MIPMAPCOUNT_BIT = 0x00020000;
    private const uint DDPF_ALPHAPIXELS = 0x00000001;
    private const uint DDPF_ALPHA = 0x00000002;
    private const uint DDPF_FOURCC = 0x00000004;
    private const uint DDPF_RGB = 0x00000040;
    private const uint DDPF_YUV = 0x00000200;
    private const uint DDPF_LUMINANCE = 0x00020000;
    private const uint DDPF_NORMAL = 0x80000000;

    public static string error;

    // DDS Texture loader inspired by
    // http://answers.unity3d.com/questions/555984/can-you-load-dds-textures-during-runtime.html#answer-707772
    // http://msdn.microsoft.com/en-us/library/bb943992.aspx
    // http://msdn.microsoft.com/en-us/library/windows/desktop/bb205578(v=vs.85).aspx
    // mipmapBias limits the number of mipmap when > 0
    public static Texture2D LoadDDS(string path, out bool hasMipMaps, bool skipFileCheck = false)
    {
        hasMipMaps = false;
        
        if (!skipFileCheck && !File.Exists(path))
        {
            error = "File does not exist";
            return null;
        }

        using var memory = new MemoryMappedFileSpanWrapper(OpenExistingMmf(path), MemoryMappedFileAccess.Read);

        var span = memory.GetSpan(0L);
        var index = 0;

        var dwMagic = ReadBytes(span, 4, ref index);

        Span<char> fourCC = stackalloc char[4];
        Encoding.ASCII.GetChars(dwMagic, fourCC);

        if (!FourCcEquals(fourCC, "DDS "))
        {
            var dwMagicArray = dwMagic.ToArray();
            error = $"Invalid DDS file. File header starting with '{
                string.Join("", dwMagicArray.Select(static b => (char)b))}' ({
                    BitConverter.ToString(dwMagicArray)}) instead of 'DDS '.";

            return null;
        }

        var dwSize = ReadUInt32(span, ref index);

        // this header byte should be 124 for DDS image files
        if (dwSize != 124u)
        {
            error = $"Invalid header size. Expected 124, got {dwSize}";
            return null;
        }

        var dwFlags = ReadUInt32(span, ref index);
        var dwHeight = ReadUInt32(span, ref index);
        var dwWidth = ReadUInt32(span, ref index);

        var dwPitchOrLinearSize = ReadUInt32(span, ref index);
        var dwDepth = ReadUInt32(span, ref index);
        var dwMipMapCount = ReadUInt32(span, ref index);

        if ((dwFlags & DDSD_MIPMAPCOUNT_BIT) == 0)
            dwMipMapCount = 1;

        // dwReserved1
        for (var i = 0; i < 11; i++)
            index += sizeof(uint); // ReadUInt32(span, ref index);

        // DDS_PIXELFORMAT
        var dds_pxlf_dwSize = ReadUInt32(span, ref index);
        var dds_pxlf_dwFlags = ReadUInt32(span, ref index);
        
        var dds_pxlf_dwFourCC = ReadBytes(span, 4, ref index);
        
        Encoding.ASCII.GetChars(dds_pxlf_dwFourCC, fourCC);
        
        var dds_pxlf_dwRGBBitCount = ReadUInt32(span, ref index);
        var pixelSize = dds_pxlf_dwRGBBitCount / 8;
        var dds_pxlf_dwRBitMask = ReadUInt32(span, ref index);
        var dds_pxlf_dwGBitMask = ReadUInt32(span, ref index);
        var dds_pxlf_dwBBitMask = ReadUInt32(span, ref index);
        var dds_pxlf_dwABitMask = ReadUInt32(span, ref index);

        // var dwCaps = ReadUInt32(span, ref index);
        // var dwCaps2 = ReadUInt32(span, ref index);
        // var dwCaps3 = ReadUInt32(span, ref index);
        // var dwCaps4 = ReadUInt32(span, ref index);
        // var dwReserved2 = ReadUInt32(span, ref index);

        TextureFormat textureFormat;
        var isCompressed = false;
        // var isNormalMap = (dds_pxlf_dwFlags & DDPF_NORMAL) != 0;

        var fourcc = (dds_pxlf_dwFlags & DDPF_FOURCC) != 0;

        var bgr888 = dds_pxlf_dwRBitMask == 0x00ff0000
            && dds_pxlf_dwGBitMask == 0x0000ff00
            && dds_pxlf_dwBBitMask == 0x000000ff;

        if (fourcc)
        {
            // Texture dos not contain RGB data, check FourCC for format
            isCompressed = true;

            textureFormat = FourCcEquals(fourCC, "DXT1") ? TextureFormat.DXT1
                : FourCcEquals(fourCC, "DXT5") ? TextureFormat.DXT5
                : FourCcEquals(fourCC, "DX10") ? TextureFormat.BC7
                : default;
        }
        else
        {
            var alpha = (dds_pxlf_dwFlags & DDPF_ALPHA) != 0;
            var rgb = (dds_pxlf_dwFlags & DDPF_RGB) != 0;
            var alphapixel = (dds_pxlf_dwFlags & DDPF_ALPHAPIXELS) != 0;
            var luminance = (dds_pxlf_dwFlags & DDPF_LUMINANCE) != 0;
            
            var rgb888 = dds_pxlf_dwRBitMask == 0x000000ff
                && dds_pxlf_dwGBitMask == 0x0000ff00
                && dds_pxlf_dwBBitMask == 0x00ff0000;

            var rgb565 = dds_pxlf_dwRBitMask == 0x0000F800
                && dds_pxlf_dwGBitMask == 0x000007E0
                && dds_pxlf_dwBBitMask == 0x0000001F;

            var argb4444 = dds_pxlf_dwABitMask == 0x0000f000
                && dds_pxlf_dwRBitMask == 0x00000f00
                && dds_pxlf_dwGBitMask == 0x000000f0
                && dds_pxlf_dwBBitMask == 0x0000000f;

            var rbga4444 = dds_pxlf_dwABitMask == 0x0000000f
                && dds_pxlf_dwRBitMask == 0x0000f000
                && dds_pxlf_dwGBitMask == 0x000000f0
                && dds_pxlf_dwBBitMask == 0x00000f00;
            
            textureFormat = rgb switch
            {
                true when (rgb888 || bgr888) // RGB or RGBA format
                    => alphapixel ? TextureFormat.RGBA32 : TextureFormat.RGB24,
                true when rgb565 // Nvidia texconv B5G6R5_UNORM
                    => TextureFormat.RGB565,
                true when alphapixel && argb4444 // Nvidia texconv B4G4R4A4_UNORM
                    => TextureFormat.ARGB4444,
                true when alphapixel && rbga4444 => TextureFormat.RGBA4444,
                false when alpha != luminance // A8 format or Luminance 8
                    => TextureFormat.Alpha8,
                _ => default
            };
        }

        if (textureFormat == default)
        {
            error
                = "Only BC7, DXT1, DXT5, A8, RGB24, BGR24, RGBA32, BGRA32, RGB565, ARGB4444 and RGBA4444 are supported";
            
            return null;
        }

        var dataBias = textureFormat != TextureFormat.BC7 ? 128 : 148;
        
        var dxtBytes = span[dataBias..];

        // Swap red and blue.
        if (!isCompressed && bgr888)
        {
            dxtBytes = dxtBytes.ToArray(); // dxtBytes otherwise pointing at readonly storage

            for (var i = 0; i + 2 < dxtBytes.Length; i += (int)pixelSize)
            {
                var b = dxtBytes[i + 0];
                var r = dxtBytes[i + 2];

                dxtBytes[i + 0] = r;
                dxtBytes[i + 2] = b;
            }
        }

        // no longer works as of unity 2022.3.35. The bug is still there tho
        // // Work around for a >Unity< Bug.
        // // if QualitySettings.masterTextureLimit != 0 (half or quarter texture rez)
        // // and dwWidth and dwHeight divided by 2 (or 4 for quarter rez) are not a multiple of 4, 
        // // and we are creating a DXT5 or DXT1 texture
        // // Then you get a Unity error on the "new Texture"
        //
        // var quality = QualitySettings.globalTextureMipmapLimit;
        //
        // // If the bug conditions are present then switch to full quality
        // if (isCompressed && quality > 0 && ((dwWidth & 3) != 0 || (dwHeight & 3) != 0))
        //     QualitySettings.globalTextureMipmapLimit = 0;

        if (isCompressed && ((dwWidth & 3) != 0 || (dwHeight & 3) != 0))
        {
            error = $"Cannot load compressed texture with non multiple of 4 dimensions of {dwWidth}x{
                dwHeight} and format {textureFormat}";
            
            return null;
        }

        try
        {
            var texture = new Texture2D((int)dwWidth, (int)dwHeight, textureFormat,
                hasMipMaps = (int)dwMipMapCount > 1);

            unsafe
            {
                fixed (byte* pData = &dxtBytes[0])
                    texture.LoadRawTextureData((IntPtr)pData, dxtBytes.Length);
            }

            return texture;
        }
        catch (Exception exception)
        {
            error = $"Exception loading texture with format '{textureFormat}', width '{
                dwWidth}', height '{dwHeight}', mipCount '{dwMipMapCount}':\n{exception}";
        }
        // finally
        // {
        //     QualitySettings.globalTextureMipmapLimit = quality;
        // }
        
        return null;
    }

    private static bool FourCcEquals(Span<char> bytes, string s) => bytes.SequenceEqual(s);

    private static uint ReadUInt32(Span<byte> bytes, ref int index)
    {
        var result = BitConverter.ToUInt32(bytes.Slice(index, sizeof(uint)));
        index += sizeof(uint);
        return result;
    }

    private static Span<byte> ReadBytes(Span<byte> bytes, int count, ref int index)
    {
        var result = bytes.Slice(index, count);
        index += count;
        return result;
    }

    private static (MemoryMappedFile file, long length) OpenExistingMmf(string path)
    {
        var info = new FileInfo(path);
        var length = info.Length;

        return (MemoryMappedFile.CreateFromFile(info.FullName, FileMode.Open, null, length,
            MemoryMappedFileAccess.Read), length);
    }
}