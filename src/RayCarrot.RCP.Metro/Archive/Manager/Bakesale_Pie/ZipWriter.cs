using System.IO;
using System.Text;
using BinarySerializer;

namespace RayCarrot.RCP.Metro.Archive.Bakesale;

// Custom ZIP writer which writes with STORE compression which the PIE archives need
public class ZipWriter : IDisposable
{
    public ZipWriter(Stream stream)
    {
        MainWriter = new Writer(stream, leaveOpen: true);
        Crc32Processor = new ChecksumCRC32Processor();
        CentralDirectoryRecordsMemoryStream = new MemoryStream();
        CentralDirectoryRecordsWriter = new Writer(CentralDirectoryRecordsMemoryStream);
    }

    private const int CrcBufferSize = 0x80000;

    private Writer MainWriter { get; }
    private ChecksumCRC32Processor Crc32Processor { get; }
    private MemoryStream CentralDirectoryRecordsMemoryStream { get; }
    private Writer CentralDirectoryRecordsWriter { get; }

    private int RecordsCount { get; set; }

    private static uint DateTimeToDosTime(DateTimeOffset dateTime)
    {
        return (uint)(
            (dateTime.Second / 2) | (dateTime.Minute << 5) | (dateTime.Hour << 11) |
            (dateTime.Day << 16) | (dateTime.Month << 21) | ((dateTime.Year - 1980) << 25));
    }

    public uint CalculateCrc32(Stream stream)
    {
        using ArrayRental<byte> crcBuffer = new(CrcBufferSize);
        Crc32Processor.Reset();
        int count;
        while ((count = stream.Read(crcBuffer.Array, 0, crcBuffer.Array.Length)) != 0)
            Crc32Processor.ProcessBytes(crcBuffer.Array, 0, count);

        return (uint)Crc32Processor.CalculatedValue;
    }

    public void WriteEntry(string name, uint fileSize, uint crc, DateTimeOffset lastWriteTime, bool isFile)
    {
        // Get the encoded name
        byte[] encodedName = Encoding.UTF8.GetBytes(name);

        // Get the time value
        uint lastWriteTimeValue = DateTimeToDosTime(lastWriteTime);

        // Get the file offset
        uint fileOffset = (uint)MainWriter.BaseStream.Position;

#pragma warning disable IDE0004
        // Write the entry
        MainWriter.Write((uint)0x04034B50); // Magic
        MainWriter.Write((ushort)10); // Version
        MainWriter.Write((ushort)0); // Bit flag
        MainWriter.Write((ushort)0); // Compression: STORE
        MainWriter.Write((uint)lastWriteTimeValue); // Last write time
        MainWriter.Write((uint)crc); // Uncompressed CRC-32
        MainWriter.Write((uint)fileSize); // Compressed size
        MainWriter.Write((uint)fileSize); // Uncompressed size
        MainWriter.Write((ushort)encodedName.Length); // Filename length
        MainWriter.Write((ushort)0); // Extra field length
        MainWriter.Write(encodedName); // Name

        // Write the central directory record entry
        CentralDirectoryRecordsWriter.Write((uint)0x02014B50); // Magic
        CentralDirectoryRecordsWriter.Write((ushort)63); // Version
        CentralDirectoryRecordsWriter.Write((ushort)10); // Minimum version
        CentralDirectoryRecordsWriter.Write((ushort)0); // Bit flag
        CentralDirectoryRecordsWriter.Write((ushort)0); // Compression: STORE
        CentralDirectoryRecordsWriter.Write((uint)lastWriteTimeValue); // Last write time
        CentralDirectoryRecordsWriter.Write((uint)crc); // Uncompressed CRC-32
        CentralDirectoryRecordsWriter.Write((uint)fileSize); // Compressed size
        CentralDirectoryRecordsWriter.Write((uint)fileSize); // Uncompressed size
        CentralDirectoryRecordsWriter.Write((ushort)encodedName.Length); // Filename length
        CentralDirectoryRecordsWriter.Write((ushort)0); // Extra field length
        CentralDirectoryRecordsWriter.Write((ushort)0); // File comment length
        CentralDirectoryRecordsWriter.Write((ushort)0); // Disk number
        CentralDirectoryRecordsWriter.Write((ushort)0); // Internal file attributes
        CentralDirectoryRecordsWriter.Write((uint)(isFile ? 0x20 : 0x10)); // External file attributes
        CentralDirectoryRecordsWriter.Write((uint)fileOffset); // Relative offset
        CentralDirectoryRecordsWriter.Write(encodedName); // Name
#pragma warning restore IDE0004

        RecordsCount++;
    }

    public void WriteCentralDirectory()
    {
        // Get the central directory offset
        uint centralDirectoryOffset = (uint)MainWriter.BaseStream.Position;

        // Copy the central directory records
        CentralDirectoryRecordsMemoryStream.Position = 0;
        CentralDirectoryRecordsMemoryStream.CopyToEx(MainWriter.BaseStream);

#pragma warning disable IDE0004
        // Write the end record
        MainWriter.Write((uint)0x06054B50); // Magic
        MainWriter.Write((ushort)0); // Disk number
        MainWriter.Write((ushort)0); // Disk start
        MainWriter.Write((ushort)RecordsCount); // Number of records on disk
        MainWriter.Write((ushort)RecordsCount); // Total number of records
        MainWriter.Write((uint)CentralDirectoryRecordsMemoryStream.Length); // Central directory size
        MainWriter.Write((uint)centralDirectoryOffset); // Central directory offset
        MainWriter.Write((ushort)0); // Comment length
#pragma warning restore IDE0004
    }

    public void Dispose()
    {
        MainWriter.Dispose();
        CentralDirectoryRecordsMemoryStream.Dispose();
        CentralDirectoryRecordsWriter.Dispose();
    }
}