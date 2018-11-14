using Bitter;
using static Bitter.BinaryWrapper;
using System.Collections.Generic;
using System;

namespace Pakman
{
    public class Index : ReadWriteInitialize
    {
        private int Version;
        public string MountPoint;
        public Dictionary<string, Meta> Entries;

        public void Init(object parameters = null)
        {
            Version = (int)parameters;
        }

        public void Read(BinaryStream bs)
        {
            try
            {
                BinaryReader r = bs.Read;
                Entries = new Dictionary<string, Meta>();

                MountPoint = r.String(r.Int());

                // read all entries
                int entries = r.Int();
                for (int i = 0; i < entries; i++)
                {
                    string name = r.String(r.Int());
                    Entries.Add(name, r.Type<Meta>(Version));
                }
            }
            catch(Exception ex)
            {
                throw new PakParseException("There was an error while parsing the index", ex);
            }
        }

        public void Write(BinaryStream bs)
        {
            BinaryWriter w = bs.Write;

            bs.Write.Int(MountPoint.Length);
            bs.Write.String(MountPoint);
            bs.Write.Int(Entries.Count);

            foreach(KeyValuePair<string, Meta> entry in Entries)
            {
                w.Int(entry.Key.Length);
                w.String(entry.Key);
                w.Type(entry.Value);
            }
        }
    }
}
