using System.IO;

namespace RayCarrot.RCP.Metro.Archive.Bakesale;

public class EncryptedPieStream : Stream
{
    #region Constructor

    public EncryptedPieStream(Stream innerStream, uint gameKey)
    {
        InnerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        GameKey = gameKey;
    }

    #endregion

    #region Private Fields

    private readonly byte[] _tempArray = new byte[1];

    #endregion

    #region Public Properties

    public Stream InnerStream { get; }
    public uint GameKey { get; }

    #endregion

    #region Private Methods

    private static void EncodeBytes(byte[] buffer, int offset, int count, long fileOffset, uint gameKey)
    {
        uint uVar2 = ((((uint)(fileOffset & 0xfffffffc) * 0x16a88000) | (((uint)(fileOffset & 0xfffffffc) * 0xcc9e2d51) >> 0x11)) * 0x1b873593) ^ gameKey;
        uVar2 = (((uVar2 << 0xd) | (uVar2 >> 0x13)) + 0xfaddaf14) * 5;
        uVar2 = ((uVar2 >> 0x10) ^ uVar2 ^ 4) * 0x85ebca6b;
        uVar2 = ((uVar2 >> 0xd) ^ uVar2) * 0xc2b2ae35;
        uVar2 = (uVar2 >> 0x10) ^ uVar2;

        uint uVar3 = (uint)(fileOffset & 3);
        uint uVar4 = (uint)(fileOffset * 0xcc9e2d51);
        
        for (int i = 0; i < count; i++)
        {
            fileOffset += 1;
            uVar4 += 0xcc9e2d51;
            buffer[offset + i] = (byte)(buffer[offset + i] ^ (byte)(uVar2 >> ((sbyte)uVar3 * 8)));
            uVar3 = (uint)(fileOffset & 3);
            if (uVar3 == 0)
            {
                uVar2 = (((uVar4 * 0x8000) | (uVar4 >> 0x11)) * 0x1b873593) ^ gameKey;
                uVar2 = (((uVar2 << 0xd) | (uVar2 >> 0x13)) + 0xfaddaf14) * 5;
                uVar2 = ((uVar2 >> 0x10) ^ uVar2 ^ 4) * 0x85ebca6b;
                uVar2 = ((uVar2 >> 0xd) ^ uVar2) * 0xc2b2ae35;
                uVar2 = (uVar2 >> 0x10) ^ uVar2;
            }
        }
    }
    protected virtual byte ProcessReadByte(byte b, long fileOffset, uint gameKey)
    {
        _tempArray[0] = b;
        EncodeBytes(_tempArray, 0, 1, fileOffset, gameKey);
        return _tempArray[0];
    }

    #endregion

    #region Stream Modifications

    // Read
    public override int Read(byte[] buffer, int offset, int count)
    {
        long pos = Position;
        int readBytes = InnerStream.Read(buffer, offset, count);

        if (readBytes != 0 && GameKey != 0)
            EncodeBytes(buffer, offset, readBytes, pos, GameKey);

        return readBytes;
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException();
    }
    public override int ReadByte()
    {
        long pos = Position;
        int v = InnerStream.ReadByte();

        if (v == -1 || GameKey == 0)
            return v;
        else
            return ProcessReadByte((byte)v, pos, GameKey);
    }

    // Write
    public override void Write(byte[] buffer, int offset, int count)
    {
        long pos = Position;

        // Encrypt
        if (GameKey != 0)
            EncodeBytes(buffer, offset, count, pos, GameKey);
        
        InnerStream.Write(buffer, offset, count);

        // Decrypt, to avoid modifying the underlying buffer
        if (GameKey != 0)
            EncodeBytes(buffer, offset, count, pos, GameKey);
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException();
    }
    public override void WriteByte(byte value)
    {
        if (GameKey != 0)
            value = ProcessReadByte(value, Position, GameKey);
        InnerStream.WriteByte(value);
    }

    #endregion

    #region Stream Redirects

    // Seek and length
    public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);
    public override void SetLength(long value) => InnerStream.SetLength(value);

    // Properties
    public override bool CanRead => InnerStream.CanRead;
    public override bool CanSeek => InnerStream.CanSeek;
    public override bool CanTimeout => InnerStream.CanTimeout;
    public override bool CanWrite => InnerStream.CanWrite;
    public override long Length => InnerStream.Length;
    public override long Position
    {
        get => InnerStream.Position;
        set => InnerStream.Position = value;
    }
    public override int ReadTimeout
    {
        get => InnerStream.ReadTimeout;
        set => InnerStream.ReadTimeout = value;
    }
    public override int WriteTimeout
    {
        get => InnerStream.WriteTimeout;
        set => InnerStream.WriteTimeout = value;
    }

    // Other
    public override object? InitializeLifetimeService() => InnerStream.InitializeLifetimeService();
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);

    // Common override methods
    public override bool Equals(object? obj) => InnerStream.Equals(obj);
    public override int GetHashCode() => InnerStream.GetHashCode();
    public override string ToString() => InnerStream.ToString();

    // Dispose and flush
    public override void Close() => InnerStream.Close();
    protected override void Dispose(bool disposing) => InnerStream.Dispose();
    public override void Flush() => InnerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => InnerStream.FlushAsync(cancellationToken);

    #endregion
}