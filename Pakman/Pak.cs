using System;
using System.IO;
using System.Security.Cryptography;
using Bitter;
using SharpCompress.Compressors.Deflate;

namespace Pakman
{
    public class Pak : BinaryWrapper
    {
        public const uint MagicNumber = 0x5A6F12E1;
        public const int EncryptionAlignment = 16;
        public const int CompressionBlockSize = 1024 * 64;

        public Info Info = new Info();
        public Index Index = new Index();

        private BinaryStream originalStream;
        private SymmetricAlgorithm encryptionAlgorithm;
        private byte[] encryptionKey;

        public Pak()
        {
            InitializeEncryption();
        }
        public Pak(string encryptionKey)
        {
            InitializeEncryption();
            SetEncryptionKey(encryptionKey);
        }
        public Pak(byte[] encryptionKey)
        {
            InitializeEncryption();
            SetEncryptionKey(encryptionKey);
        }
        
        public void SetEncryptionKey(string encryptionKey)
        {
            encryptionAlgorithm.Key = Util.StringToByteArray(encryptionKey);
            this.encryptionKey = encryptionAlgorithm.Key;
        }
        public void SetEncryptionKey(byte[] encryptionKey)
        {
            encryptionAlgorithm.Key = encryptionKey;
            this.encryptionKey = encryptionAlgorithm.Key;
        }

        private void InitializeEncryption()
        {
            encryptionAlgorithm = new RijndaelManaged();
            encryptionAlgorithm.Mode = CipherMode.ECB;
            encryptionAlgorithm.Padding = PaddingMode.None;
            encryptionAlgorithm.IV = new byte[16];
            encryptionAlgorithm.BlockSize = 128;
        }


        public override void Read(BinaryStream bs)
        {

            keepReadOpen = true;
            originalStream = bs;

            // we need to read the footer first
            bs.ByteOffset = bs.Length - 44;
            Info = bs.Read.Type<Info>();

            if(Info.IsIndexEncrypted && (encryptionKey == null || encryptionKey.Length == 0))
            {
                throw new PakDecryptionException("The index in this archive is encrypted but no key was provided to decrypt it");
            }

            // seek to the start of the index
            bs.ByteOffset = (long)Info.IndexOffset;
            BinaryStream ibs = null;
            try
            {
                byte[] indexData = bs.Read.ByteArray((int)Info.IndexSize);
                if (Info.IsIndexEncrypted)
                {
                    indexData = Decrypt(indexData);
                }
                ibs = new BinaryStream(new MemoryStream(indexData));
            }
            catch (Exception ex)
            {
                throw new PakStreamException("Could not read the index", ex);
            }

            try
            {
                Index = ibs.Read.Type<Index>(Info.Version);
            }
            catch(Exception ex)
            {
                if(Info.IsIndexEncrypted)
                {
                    throw new PakDecryptionException("There was an error decrypting the index, are you sure the provided key is correct?", ex);
                }
                else
                {
                    throw ex;
                }
            }

        }
        public override void Write(BinaryStream bs)
        {
            
        }

        public byte[] Unpack(string entry)
        {
            if(!Index.Entries.ContainsKey(entry))
            {
                throw new PakException("There is no entry in the archive with that name");
            }
            return Unpack(Index.Entries[entry]);
        }
        public byte[] Unpack(Meta meta)
        {
            if (meta.CompressionMethod == CompressionMethod.Custom)
            {
                throw new Exception("Cant decompress using custom algorithms (yet)");
            }
            else if (meta.CompressionMethod == CompressionMethod.None)
            {
                originalStream.ByteOffset = meta.Offset + meta.MetaSize;
                byte[] data = originalStream.Read.ByteArray((int)(meta.Size));
                if(meta.IsEncrypted)
                {
                    data = Decrypt(data);
                }
                return data;
            }
            else
            {
                CompressionLevel cl = CompressionLevel.Default;
                if (meta.CompressionBias == CompressionBias.OverridePlatform)
                {
                    throw new Exception("Cant decompress using custom OverridePlatform flag (yet)");
                }
                else if (meta.CompressionBias == CompressionBias.Memory)
                {
                    cl = CompressionLevel.BestCompression;
                }
                else if (meta.CompressionBias == CompressionBias.Speed)
                {
                    cl = CompressionLevel.BestSpeed;
                }

                MemoryStream payload;
                MemoryStream output = new MemoryStream((int)meta.UncompressedSize);

                // make sure all offsets relative
                long relativeOffset = (Info.Version < Version.RelativeChunkOffsets ? meta.Offset : 0) + meta.CompressionBlocks.BlockStartOffset();

                originalStream.ByteOffset = meta.Offset + meta.MetaSize;
                byte[] compressedData = originalStream.Read.ByteArray((int)meta.Size);

                if (meta.IsEncrypted)
                {
                    compressedData = Decrypt(compressedData);
                }
                
                BinaryStream compressedStream = new BinaryStream(new MemoryStream(compressedData));

                foreach (CompressionBlock block in meta.CompressionBlocks)
                {
                    compressedStream.ByteOffset = block.Start - relativeOffset;
                    payload = new MemoryStream(compressedStream.Read.ByteArray((int)block.Length));

                    if (meta.CompressionMethod == CompressionMethod.ZLIB)
                    {
                        using (ZlibStream zl = new ZlibStream(payload, SharpCompress.Compressors.CompressionMode.Decompress, cl))
                        {
                            zl.CopyTo(output);
                        }
                    }
                    else // gzip
                    {
                        using (GZipStream gz = new GZipStream(payload, SharpCompress.Compressors.CompressionMode.Decompress, cl))
                        {
                            gz.CopyTo(output);
                        }
                    }
                }
                return output.ToArray();
            }

        }
        private byte[] Decrypt(byte[] data)
        {
            return Decrypt(encryptionAlgorithm, data);
        }
        private byte[] Decrypt(SymmetricAlgorithm algorithm, byte[] data)
        {
            if (algorithm == null)
            {
                throw new Exception("No algorithm was provided");
            }

            if (algorithm.Key == null || algorithm.Key.Length == 0)
            {
                throw new Exception("No encryption key was provided");
            }

            if(data == null || data.Length == 0)
            {
                throw new Exception("No data was provided to decrypt");
            }

            using (MemoryStream output = new MemoryStream())
            using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            using (CryptoStream cryptoStream = new CryptoStream(output, decryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);

                int rest = (int)Align(data.Length, EncryptionAlignment) - data.Length;
                if (rest != 0)
                {
                    cryptoStream.Write(new byte[rest], 0, rest);
                }

                cryptoStream.FlushFinalBlock();
                output.Position = 0;
                output.Read(data, 0, data.Length);
                return data;
            }
        }
        private long Align(long pointer, int alignment)
        {
            return (pointer + alignment - 1) & ~(alignment - 1);
        }

    }
}
