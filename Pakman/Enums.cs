using System;

namespace Pakman
{
    public enum Version
    {
        Initial = 1,
        NoTimestamps = 2,
        CompressionEncryption = 3,
        IndexEncryption = 4,
        RelativeChunkOffsets = 5,
        DeleteRecords = 6,
        EncryptionKeyGuid = 7
    }

    public enum CompressionMethod : uint
    {
        None = 0x00,
        ZLIB = 0x01,
        GZIP = 0x02,
        Custom = 0x04
    };

    public enum CompressionBias : uint
    {
        Default = 0x00,
        Memory = 0x10,
        Speed = 0x20,
        OverridePlatform = 0x40
    }

    [Flags]
    public enum EntryFlags : byte
    {
        None = 0x00,
        Encrypted = 0x01,
        Deleted = 0x02
    }
}
