using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Verse;

namespace GraphicSetter;
public static class DDSLoader
{
    public static string error; // return null, handle texture outside
    public static string warning; // return dds loaded by unity, may be decompressed

    private const uint
        DDSD_MIPMAPCOUNT_BIT = 0x00020000,//Flag 
        DDS_MAGIC = 0x20534444;
    #region Header
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DDS_HEADER
    {
        public uint dwSize;
        public uint dwFlags;
        public int dwHeight;
        public int dwWidth;
        public uint dwPitchOrLinearSize;
        public uint dwDepth;
        public uint dwMipMapCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public uint[] dwReserved1;
        public DDS_PIXELFORMAT ddspf;
        public uint dwCaps;
        public uint dwCaps2;
        public uint dwCaps3;
        public uint dwCaps4;
        public uint dwReserved2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DDS_PIXELFORMAT
    {
        public uint dwSize;
        public uint dwFlags;
        public FourCC dwFourCC;
        public uint dwRGBBitCount;
        public uint dwRBitMask;
        public uint dwGBitMask;
        public uint dwBBitMask;
        public uint dwABitMask;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DDS_HEADER_DXT10
    {
        public DXGI_FORMAT dxgiFormat;
        public uint resourceDimension;
        public uint miscFlag;          // DDS_RESOURCE_MISC_FLAG
        public uint arraySize;
        public uint miscFlags2;        // DDS_MISC_FLAGS2
    }
    public enum DXGI_FORMAT : uint
    {
        DXGI_FORMAT_UNKNOWN = 0,
        DXGI_FORMAT_R8G8B8A8_UNORM = 28,
        DXGI_FORMAT_BC1_UNORM = 71,  // DXT1
        DXGI_FORMAT_BC2_UNORM = 74,  // DXT3
        DXGI_FORMAT_BC3_UNORM = 77,  // DXT5
        DXGI_FORMAT_BC4_UNORM = 80,  // ATI1
        DXGI_FORMAT_BC5_UNORM = 83,  // ATI2
        DXGI_FORMAT_BC6H_UF16 = 95,
        DXGI_FORMAT_BC7_UNORM = 98,
        DXGI_FORMAT_BC7_UNORM_SRGB = 99
        // etc...
    }
    public enum FourCC : uint
    {
        // Standard DXT Format
        DXT1 = 0x31545844, // 'DXT1' - BC1
        DXT2 = 0x32545844, // 'DXT2' - BC1 * Alpha
        DXT3 = 0x33545844, // 'DXT3' - BC2 
        DXT4 = 0x34545844, // 'DXT4' - BC3 * Alpha
        DXT5 = 0x35545844, // 'DXT5' - BC3 
        // DX10 Ext
        DX10 = 0x30315844, // 'DX10' - Extended
        
        NONE = 0x00000000, // No Compression
    }

    #endregion

    public static Texture2D LoadDDS(FileStream fileStream, out bool hasMipMaps)
    {
        error = null;
        warning = null;
        hasMipMaps = false;

        using (BinaryReader reader = new BinaryReader(fileStream))
        {
            Texture2D texture2D;
            int dxtBytesLength = 0;
            bool modifiedNon4 = false;
            DeepProfiler.Start("Reading DDS Info");
            try
            {
                uint magic = reader.ReadUInt32();
                if (magic != DDS_MAGIC) // "DDS "
                {
                    error = "Invalid DDS file ";
                    return null;
                }
                DDS_HEADER header = ReadStructure<DDS_HEADER>(reader);

                if (header.dwSize != 124u)
                {
                    error = "Invalid header size";
                    return null;
                }
                int width = header.dwWidth,
                    height = header.dwHeight;


                bool isDX10 = header.ddspf.dwFourCC == FourCC.DX10;
                hasMipMaps = (header.dwFlags & DDSD_MIPMAPCOUNT_BIT) != 0 && header.dwMipMapCount > 1u;
                DDS_HEADER_DXT10 headerdx10 = default;
                GraphicsFormat graphicsFormat;
                if (isDX10)
                {
                    headerdx10 = ReadStructure<DDS_HEADER_DXT10>(reader);
                    graphicsFormat = headerdx10.dxgiFormat switch
                    {
                        DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM => GraphicsFormat.RGBA_DXT1_UNorm,
                        DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM => GraphicsFormat.RGBA_DXT3_UNorm,
                        DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM => GraphicsFormat.RGBA_DXT5_UNorm,
                        DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM =>
                            GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.BC4, false),
                        DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM =>
                            GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.BC5, false),
                        DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16 => GraphicsFormat.RGB_BC6H_UFloat,
                        DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM => GraphicsFormat.RGBA_BC7_UNorm,
                        DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB => GraphicsFormat.RGBA_BC7_SRGB,
                        _ => GraphicsFormat.R8G8B8A8_SNorm

                    };
                }
                else
                {
                    graphicsFormat = header.ddspf.dwFourCC switch
                    {
                        FourCC.DXT1 => GraphicsFormat.RGBA_DXT1_UNorm,
                        FourCC.DXT3 => GraphicsFormat.RGBA_DXT3_UNorm,
                        FourCC.DXT5 => GraphicsFormat.RGBA_DXT5_UNorm,
                        _ => GraphicsFormat.R8G8B8A8_SNorm,
                    };
                }

                if ((height & 3) != 0 || (width & 3) != 0)
                {
                    var _w = (width + 3) & ~3;
                    var _h = (height + 3) & ~3;
                    dxtBytesLength = _w * _h;

                    if (header.ddspf.dwFourCC == FourCC.DXT1)
                        dxtBytesLength >>= 1;
                    if (header.ddspf.dwFourCC != FourCC.NONE && dxtBytesLength <= reader.BaseStream.Length - reader.BaseStream.Position)
                    {
                        modifiedNon4 = true;
                        height = _h;
                        width = _w;
                        if (header.dwMipMapCount > 1)
                        {
                            header.dwMipMapCount = 1;
                        }
                    }
                    else
                    {
                        warning = "Not dividable by 4,try Fallback";
                        goto FallBack;
                    }

                }


                if (graphicsFormat == GraphicsFormat.R8G8B8A8_SNorm)
                {
                    warning = $"Unknow Format {(!headerdx10.Equals(default) ? "dxgiFormat:" + headerdx10.dxgiFormat : "graphicFormat:" + graphicsFormat)}, try FallBack";
                    goto FallBack;
                }
                texture2D = new Texture2D(width, height, graphicsFormat, (int)header.dwMipMapCount, TextureCreationFlags.None);
                goto NormalRead;
            }
            finally
            {

                DeepProfiler.End();
            }


        NormalRead:


            DeepProfiler.Start("Loading DDS Data To Ram");
            try
            {
                if (!modifiedNon4)
                {
                    dxtBytesLength = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                    uint MipChainSize = GraphicsFormatUtility.ComputeMipChainSize(texture2D.width, texture2D.height, texture2D.graphicsFormat, texture2D.mipmapCount);
                    if (MipChainSize > dxtBytesLength)
                    {
                        warning = "Not Enough Data For Mipmap, try FallBack";
                        goto FallBack;
                    }
                }

                texture2D.LoadRawTextureData(reader.ReadBytes(dxtBytesLength));
                fileStream.Close();
                return texture2D;
            }
            finally
            {
                DeepProfiler.End();

            }
        FallBack:
            DeepProfiler.Start("FallBack Load DDS");
            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                fileStream.Seek(0, SeekOrigin.Begin);
                texture.LoadImage(reader.ReadBytes((int)fileStream.Length));
                fileStream.Close();
                return texture;
            }
            finally
            {
                DeepProfiler.End();
            }
        }
    }


    private static T ReadStructure<T>(BinaryReader reader) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        byte[] bytes = reader.ReadBytes(size);

        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        }
        finally
        {
            handle.Free();
        }
    }

}