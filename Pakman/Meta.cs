using Bitter;
using System;
using static Bitter.BinaryWrapper;

namespace Pakman
{
    public class Meta : ReadWriteInitialize
    {
        private Version version;
        public long Offset;
        public long Size;
        public long UncompressedSize;
        public CompressionMethod CompressionMethod;
        public CompressionBias CompressionBias;
        public long Timestamp;
        public byte[] Hash;
        public CompressionBlocks CompressionBlocks;
        public EntryFlags Flags;
        public int CompressionBlockSize;

        public bool IsEncrypted
        {
            get
            {
                return (Flags & EntryFlags.Encrypted) == EntryFlags.Encrypted;
            }
            set
            {
                if(value)
                {
                    Flags |= EntryFlags.Encrypted;
                }
                else
                {
                    Flags &= ~EntryFlags.Encrypted;
                }
            }
        }
        public bool IsDeleted
        {
            get
            {
                return (Flags & EntryFlags.Deleted) == EntryFlags.Deleted;
            }
            set
            {
                if (value)
                {
                    Flags |= EntryFlags.Deleted;
                }
                else
                {
                    Flags &= ~EntryFlags.Deleted;
                }
            }
        }

        public int MetaSize
        {
            get
            {
                int size = sizeof(long) + sizeof(long) + sizeof(long) + sizeof(int) + 20;
                if (version < Version.NoTimestamps)
                {
                    size += sizeof(long);
                }
                if (version >= Version.CompressionEncryption)
                {
                    if (CompressionMethod != CompressionMethod.None)
                    {
                        size += sizeof(int) + (sizeof(long) * 2 * CompressionBlocks.Count);
                    }
                    size += sizeof(byte) + sizeof(int);
                }
                return size;
            }
        }


        public void Init(object parameters = null)
        {
            version = (Version)parameters;
        }

        public void Read(BinaryStream bs)
        {
            try
            {
                BinaryReader r = bs.Read;
                Offset = r.Long();
                Size = r.Long();
                UncompressedSize = r.Long();

                byte compressionFlags = (byte)r.UInt();
                CompressionMethod = (CompressionMethod) (compressionFlags & 0x0F);
                CompressionBias = (CompressionBias) (compressionFlags & 0xF0);

                if (version < Version.NoTimestamps)
                {
                    Timestamp = r.Long();
                }

                Hash = r.ByteArray(20);

                if (version >= Version.CompressionEncryption)
                {
                    if (CompressionMethod != CompressionMethod.None)
                    {
                        CompressionBlocks = r.Type<CompressionBlocks>();
                    }
                    Flags = (EntryFlags)r.Byte();
                    CompressionBlockSize = r.Int();
                }
            }
            catch (Exception ex)
            {
                throw new PakParseException("There was an error while parsing file metadata", ex);
            }
        }

        public void Write(BinaryStream bs)
        {
            BinaryWriter w = bs.Write;

            w.Long(Offset);
            w.Long(Size);
            w.Long(UncompressedSize);
            w.UInt((uint)CompressionMethod + (uint)CompressionBias);

            if (version < Version.NoTimestamps)
            {
                w.Long(Timestamp);
            }

            w.ByteArray(Hash);

            if (version >= Version.CompressionEncryption)
            {
                if (CompressionMethod != CompressionMethod.None)
                {
                    w.Int(CompressionBlocks.Count);
                    w.TypeList(CompressionBlocks);
                }
                w.Byte((byte)Flags);
                w.Int(CompressionBlockSize);
            }
        }

    }
}
