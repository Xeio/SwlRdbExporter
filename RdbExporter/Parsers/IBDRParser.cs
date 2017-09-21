using RdbExporter.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RdbExporter.Parsers
{
    public static class IBDRParser
    {
        public static List<IDBRIndexEntrty> ParseIBDRFile(string file)
        {
            using (var binaryReader = new BinaryReader(File.OpenRead(file)))
            {
                var header = Encoding.ASCII.GetString(binaryReader.ReadBytes(4));
                if (header != "IBDR") throw new InvalidOperationException("Not a IBDR file.");

                var versionNumber = binaryReader.ReadInt32();
                var hash = binaryReader.ReadBytes(16);
                var entryCount = binaryReader.ReadInt32();

                var rdbTypes = ReadIds(binaryReader, entryCount).ToList().ReadFileEntries(binaryReader).ToList();

                //There's a bunch of extra data to parse at the end of the file here, do we need it?

                return rdbTypes;
            }
        }

        private static IEnumerable<IDBRIndexEntrty> ReadIds(BinaryReader binaryReader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new IDBRIndexEntrty() {
                    Type = binaryReader.ReadInt32(),
                    Id = binaryReader.ReadInt32()
                };
            }
        }

        private static IEnumerable<IDBRIndexEntrty> ReadFileEntries(this IEnumerable<IDBRIndexEntrty> ids, BinaryReader binaryReader)
        {
            foreach(var id in ids)
            {
                id.FileNumber = binaryReader.ReadByte();
                binaryReader.ReadBytes(3);
                id.FileOffset = binaryReader.ReadInt32();
                id.FileLength = binaryReader.ReadInt32();
                binaryReader.ReadBytes(16);

                if (id.FileNumber != 255)
                {
                    //255.rdb doesn't exist, not sure if we should handle these somehow
                    yield return id;
                }
            }
        }
    }
}
