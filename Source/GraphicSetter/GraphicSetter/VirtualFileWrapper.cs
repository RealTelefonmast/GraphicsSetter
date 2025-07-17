using System.IO;
using RimWorld.IO;

namespace GraphicSetter;

public class VirtualFileWrapper : VirtualFile
{
    private readonly FileInfo fileInfo;

    public VirtualFileWrapper(FileInfo info)
    {
        fileInfo = info;
    }

    public override Stream CreateReadStream()
    {
        return fileInfo.OpenRead();
    }

    public override string ReadAllText()
    {
        return File.ReadAllText(fileInfo.FullName);
    }

    public override string[] ReadAllLines()
    {
        return File.ReadAllLines(fileInfo.FullName);
    }

    public override byte[] ReadAllBytes()
    {
        return File.ReadAllBytes(fileInfo.FullName);
    }

    public override string ToString()
    {
        return string.Format("FilesystemFile [{0}], Length {1}", FullPath, fileInfo.Length.ToString());
    }


    public override string Name => fileInfo.Name;

    public override string FullPath => fileInfo.FullName;

    public override bool Exists => fileInfo.Exists;

    public override long Length => fileInfo.Length;
}