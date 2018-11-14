using System;

namespace Pakman
{
    public class PakDecryptionException : Exception
    {
        public PakDecryptionException()
        {
        }

        public PakDecryptionException(string message)
            : base(message)
        {
        }

        public PakDecryptionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class PakParseException : Exception
    {
        public PakParseException()
        {
        }

        public PakParseException(string message)
            : base(message)
        {
        }

        public PakParseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class PakStreamException : Exception
    {
        public PakStreamException()
        {
        }

        public PakStreamException(string message)
            : base(message)
        {
        }

        public PakStreamException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class PakException : Exception
    {
        public PakException()
        {
        }

        public PakException(string message)
            : base(message)
        {
        }

        public PakException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
