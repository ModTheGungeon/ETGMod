using UnityEngine; // Replace with System in non-Unity context
using Ionic.Zip;
using System.Collections.Generic;
using System.IO;
using IOFile = System.IO.File;
using System.Reflection;

/// <summary>
/// ETGMod asset metadata.
/// </summary>
public class AssetMetadata {

    public EType Type;
    public System.Type AssetType = null;

    public string File;

    public string Zip;

    public Assembly Assembly;
    public string AssemblyName;

    public long Offset;
    public int Length;

    /// <summary>
    /// Returns a new stream to read the data from.
    /// In case of limited data (Length is set), LimitedStream is used.
    /// </summary>
    public Stream Stream {
        get {
            Stream stream = null;
            if (Type == EType.File) {
                stream = IOFile.OpenRead(File);
            } else if (Type == EType.Zipped) {
                string file = File.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                using (ZipFile zip = ZipFile.Read(Zip)) {
                    foreach (ZipEntry entry in zip.Entries) {
                        if (entry.FileName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) == file) {
                            MemoryStream ms = new MemoryStream();
                            entry.Extract(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            stream = ms;
                        }
                    }
                }
            } else if (Type == EType.Assembly) {
                stream = Assembly.GetManifestResourceStream(File);
            }

            if (stream == null || Length == 0) {
                return stream;
            }
            return new LimitedStream(stream, Offset, Length);
        }
    }

    /// <summary>
    /// Returns the file's contents.
    /// </summary>
    public byte[] Data {
        get {
            using (Stream stream = Stream) {
                if (stream is LimitedStream) {
                    return ((LimitedStream) stream).GetBuffer();
                }

                using (MemoryStream ms = new MemoryStream()) {
                    byte[] buffer = new byte[2048];
                    int read;
                    while (0 < (read = stream.Read(buffer, 0, buffer.Length))) {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }
    }

    public AssetMetadata() {
        Type = EType.File;
    }

    public AssetMetadata(string file)
        : this(file, 0, 0) {
    }
    public AssetMetadata(string file, long offset, int length)
        : this() {
        File = file;
        Offset = offset;
        Length = length;
    }

    public AssetMetadata(string zip, string file)
        : this(file) {
        Type = EType.Zipped;
        Zip = zip;
        File = file;
    }

    public AssetMetadata(Assembly assembly, string file)
        : this(file) {
        Type = EType.Assembly;
        Assembly = assembly;
        AssemblyName = assembly.GetName().Name;
    }

    public enum EType {
        File,
        Zipped,
        Assembly
    }

}
