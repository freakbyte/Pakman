using Bitter;
using System;
using static Bitter.BinaryWrapper;

namespace Pakman
{
    public class Info : ReadWrite
    {
        public uint Magic;
        public Version Version;
        public ulong IndexOffset;
        public ulong IndexSize;
        public byte[] Hash;
        public bool IsIndexEncrypted;
        public Guid EncryptionKeyGuid;

        public void Read(BinaryStream bs)
        {
            try
            {
                Magic = bs.Read.UInt();
                Version = (Version)bs.Read.Int();
                IndexOffset = bs.Read.ULong();
                IndexSize = bs.Read.ULong();
                Hash = bs.Read.ByteArray(20);

                if (Version >= Version.IndexEncryption)
                {
                    bs.ByteOffset = bs.Length - (44 + 1);
                    IsIndexEncrypted = bs.Read.Byte() == 1;
                }

                EncryptionKeyGuid = Guid.Empty;
                if (Version >= Version.EncryptionKeyGuid)
                {
                    bs.ByteOffset = bs.Length - (44 + 1 + 16);
                    EncryptionKeyGuid = new Guid(bs.Read.ByteArray(16));
                }
            }
            catch(Exception ex)
            {
                throw new PakParseException("There was an error while parsing the archive info", ex);
            }

            if (Magic != Pak.MagicNumber)
            {
                throw new PakParseException("Magic numbers do not match, this is not a valid pak file");
            }
        }

        public void Write(BinaryStream bs)
        {
            if (Version >= Version.EncryptionKeyGuid)
            {
                bs.Write.ByteArray(EncryptionKeyGuid.ToByteArray());
            }

            if (Version >= Version.IndexEncryption)
            {
                bs.Write.Byte((byte)(IsIndexEncrypted ? 1 : 0));
            }

            bs.Write.UInt(Magic);
            bs.Write.Int((int)Version);
            bs.Write.ULong(IndexOffset);
            bs.Write.ULong(IndexSize);
            bs.Write.ByteArray(Hash);
        }
    }
}
