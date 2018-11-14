using Bitter;
using System;
using System.Collections.Generic;
using static Bitter.BinaryWrapper;

namespace Pakman
{
    public class CompressionBlocks : List<CompressionBlock>, ReadWrite
    {
        public void Read(BinaryStream bs)
        {
            try
            {
                this.AddRange(bs.Read.TypeList<CompressionBlock>(bs.Read.Int()));
            }
            catch(Exception ex)
            {
                throw new PakParseException("There was an error while parsing entrys compression blocks");
            }
        }
        public long BlockStartOffset()
        {
            if(this.Count == 0)
            {
                throw new PakException("We dont have any compression blocks :o");
            }
            return this[0].Start;
        }
        public long BlockEndOffset()
        {
            if (this.Count == 0)
            {
                throw new PakException("We dont have any compression blocks :o");
            }
            return this[this.Count-1].End;
        }
        public long TotalBlockLength()
        {
            return BlockEndOffset() - BlockStartOffset();
        }

        public void Write(BinaryStream bs)
        {
            bs.Write.TypeList(this);
        }
    }
}
