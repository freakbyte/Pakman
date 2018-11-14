using Bitter;
using static Bitter.BinaryWrapper;

namespace Pakman
{

    public class CompressionBlock : ReadWrite
    {
        public long Start;
        public long End;
        public long Length
        {
            get
            {
                return End - Start;
            }
        }
        public void Read(BinaryStream bs)
        {
            Start = bs.Read.Long();
            End = bs.Read.Long();
        }

        public void Write(BinaryStream bs)
        {
            bs.Write.Long(Start);
            bs.Write.Long(End);
        }
    }
}
