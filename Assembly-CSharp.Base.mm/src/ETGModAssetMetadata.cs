using Ionic.Zip;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// ETGMod asset metadata.
/// </summary>
public class ETGModAssetMetadata {

    public string Zip;
    public string File;
    public long Offset;
    public int Length;

    /// <summary>
    /// Returns a new stream to read the data from.
    /// In case of limited data (Length is set), LimitedStream is used.
    /// </summary>
    public Stream Stream {
        get {
            Stream stream = null;
            if (Zip == null) {
                stream = System.IO.File.OpenRead(File);
            } else {
                using (ZipFile zip = ZipFile.Read(Zip)) {
                    foreach (ZipEntry entry in zip.Entries) {
                        if (entry.FileName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) == File) {
                            MemoryStream ms = new MemoryStream();
                            entry.Extract(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            stream = ms;
                        }
                    }
                }
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

    public ETGModAssetMetadata() {
    }

    public ETGModAssetMetadata(string file)
        : this(file, 0, 0) {
    }
    public ETGModAssetMetadata(string file, long offset, int length)
        : this() {
        File = file;
        Offset = offset;
        Length = length;
    }

    public ETGModAssetMetadata(string zip, string file)
        : this() {
        Zip = zip;
        File = file;
    }

}
