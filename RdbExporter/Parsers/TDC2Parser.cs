using RdbExporter.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RdbExporter.Parsers
{
    public static class TDC2Parser
    {
        public static TDC2File ParseTDC2File(string file)
        {
            using var binaryReader = new BinaryReader(File.OpenRead(file));
            return ParseTDC2File(binaryReader);
        }

        public static TDC2File ParseTDC2File(BinaryReader binaryReader)
        {
            try
            {
                var header = Encoding.ASCII.GetString(binaryReader.ReadBytes(4));
                if (header != "TDC2") throw new InvalidOperationException("Not a TDC2 file.");

                var category = binaryReader.ReadInt32();
                var flags = binaryReader.ReadInt32();
                var stringsByteLength = binaryReader.ReadInt32();
                var numStrings = binaryReader.ReadInt32();

                var hash = binaryReader.ReadBytes(16);
                var stringData = binaryReader.ReadBytes(stringsByteLength);

                var stringEntries = ReadEntries(binaryReader, stringData, numStrings).ToList();

                return new TDC2File()
                {
                    Category = category,
                    Entries = stringEntries
                };
            }
            finally
            {
                binaryReader.Dispose();
            }
        }

        private static IEnumerable<TDC2Entry> ReadEntries(BinaryReader stream, byte[] stringData, int numStrings)
        {
            for (int i = 0; i < numStrings; i++)
            {
                var id = stream.ReadInt32();
                //Seems like this value might be something related to localization, mostly filled out in the items files for FR and DE clients. Mostly with the same ID in all places.
                _ = stream.ReadInt32(); //Unknown value
                var offset = stream.ReadInt32();
                var length = stream.ReadInt32();
                if (length == 0) continue;
                yield return new TDC2Entry() { ID = id, Value = Encoding.UTF8.GetString(stringData, offset, length) };
            }
        }
    }
}
