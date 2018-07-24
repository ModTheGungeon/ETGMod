using System;
using System.IO;

public class LimitedStream : MemoryStream {
    
    public Stream LimitStream;
    public long LimitOffset;
    public long LimitLength;
    
    public bool LimitStreamShared = false;
    private long _Position = 0;
    
    protected byte[] CachedBuffer;
    protected long CachedOffset;
    protected long CachedLength;
    private bool _CacheBuffer = true;
    public bool CacheBuffer {
        get {
            return _CacheBuffer;
        }
        set {
            if (!value) {
                CachedBuffer = null;
            }
            _CacheBuffer = value;
        }
    }
    
    public override bool CanRead {
        get {
            return LimitStream.CanRead;
        }
    }

    public override bool CanSeek {
        get {
            return LimitStream.CanSeek;
        }
    }

    public override bool CanWrite {
        get {
            return LimitStream.CanWrite;
        }
    }

    public override long Length {
        get {
            return LimitLength;
        }
    }

    public override long Position {
        get {
            return LimitStreamShared ? _Position : LimitStream.Position - LimitOffset;
        }
        set {
            LimitStream.Position = value + LimitOffset;
            _Position = value;
        }
    }
    
    public LimitedStream(Stream stream, long offset, long length)
        : base() {
        LimitStream = stream;
        LimitOffset = offset;
        LimitLength = length;
        LimitStream.Seek(offset, SeekOrigin.Begin);
    }
    
    public override void Flush() {
        LimitStream.Flush();
    }
    
    public override int Read(byte[] buffer, int offset, int count) {
        if (LimitOffset + LimitLength <= Position + count) {
            throw new Exception("out of something");
        }
        int read = LimitStream.Read(buffer, offset, count);
        _Position += read;
        return read;
    }
    
    public override int ReadByte() {
        if (LimitOffset + LimitLength <= Position + 1) {
            throw new Exception("out of something");
        }
        int b = LimitStream.ReadByte();
        if (b != -1) {
            _Position++;
        }
        return b;
    }

    public override long Seek(long offset, SeekOrigin origin) {
        switch (origin) {
            case SeekOrigin.Begin:
                if (LimitOffset + LimitLength <= offset) {
                    throw new Exception("out of something");
                }
                _Position = offset;
                return LimitStream.Seek(LimitOffset + offset, SeekOrigin.Begin);
            case SeekOrigin.Current:
                if (LimitOffset + LimitLength <= Position + offset) {
                    throw new Exception("out of something");
                }
                _Position += offset;
                return LimitStream.Seek(offset, SeekOrigin.Current);
            case SeekOrigin.End:
                if (LimitLength - offset < 0) {
                    throw new Exception("out of something");
                }
                _Position = LimitLength - offset;
                return LimitStream.Seek(LimitOffset + LimitLength - offset, SeekOrigin.Begin);
            default:
                return 0;
        }
    }

    public override void SetLength(long value) {
        if (LimitStreamShared) {
            LimitLength = value;
            return;
        }
        LimitStream.SetLength(LimitOffset + value + LimitLength);
    }

    public override void Write(byte[] buffer, int offset, int count) {
        if (LimitOffset + LimitLength <= Position + count) {
            throw new Exception("out of something");
        }
        LimitStream.Write(buffer, offset, count);
        _Position += count;
    }
    
    public override byte[] GetBuffer() {
        if (CachedBuffer != null && CachedOffset == LimitOffset && CachedLength == LimitLength) {
            return CachedBuffer;
        }
            
        if (!_CacheBuffer) {
            return ToArray();
        }
            
        CachedOffset = LimitOffset;
        CachedLength = LimitLength;
        return CachedBuffer = ToArray();
    }
    
    private readonly byte[] _ToArrayReadBuffer = new byte[2048];
    public override byte[] ToArray() {
        byte[] buffer;
        int read;
        
        long origPosition = LimitStream.Position;
        LimitStream.Seek(LimitOffset, SeekOrigin.Begin);
        
        long length = LimitLength == 0 ? LimitStream.Length : LimitLength;
        
        if (length == 0) {
            //most performant way would be to use the base MemoryStream, but
            //System.NotSupportedException: Stream does not support writing.
            MemoryStream ms = new MemoryStream();
                
            LimitStream.Seek(LimitOffset, SeekOrigin.Begin);
            while (0 < (read = LimitStream.Read(_ToArrayReadBuffer, 0, _ToArrayReadBuffer.Length))) {
                base.Write(_ToArrayReadBuffer, 0, read);
            }
                
            LimitStream.Seek(origPosition, SeekOrigin.Begin);
            buffer = base.ToArray();
            ms.Close();
            return buffer;
        }
            
        buffer = new byte[length];
        int readCompletely = 0;
        while (readCompletely < length) {
            read = LimitStream.Read(buffer, readCompletely, buffer.Length - readCompletely);
            readCompletely += read;
        }
            
        LimitStream.Seek(origPosition, SeekOrigin.Begin);
        return buffer;
    }
        
    public override void Close() {
        base.Close();
        if (!LimitStreamShared) {
            LimitStream.Close();
        }
    }
        
    protected override void Dispose(bool disposing) {
        if (!LimitStreamShared) {
            LimitStream.Dispose();
            LimitStream.Close();
        }
        base.Dispose(disposing);
    }
        
}