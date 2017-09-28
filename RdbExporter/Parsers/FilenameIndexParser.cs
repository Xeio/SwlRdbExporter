using RdbExporter.Utilities;
using System.Collections.Generic;
using System.IO;

namespace RdbExporter.Parsers
{
    public class FilenameIndexParser
    {
        public static Dictionary<int, Dictionary<int, string>> ParseNameIndex(BinaryReader reader)
        {
            //var header = Encoding.ASCII.GetString(reader.ReadBytes(8)); //0x11,0x00,0x00,0x00,0x51,0x69,0x0F,0x00 Dunno if this is significant

            var rdbTypesCount = reader.ReadInt32();

            return ReadRdbTypes(reader, rdbTypesCount);
        }

        private static Dictionary<int, Dictionary<int, string>> ReadRdbTypes(BinaryReader reader, int count)
        {
            var rdbs = new Dictionary<int, Dictionary<int, string>>(count);
            for (int i = 0; i < count; i++)
            {
                var rdbType = reader.ReadInt32();
                var rdbEntryCount = reader.ReadInt32();
                rdbs[rdbType] = ReadFilenames(reader, rdbEntryCount);
            }
            return rdbs;
        }

        private static Dictionary<int, string> ReadFilenames(BinaryReader reader, int count)
        {
            var filenames = new Dictionary<int, string>(count);
            for (int i = 0; i < count; i++)
            {
                var fileId = reader.ReadInt32();
                var name = reader.ReadInt32PrefacedString(false).TrimEnd('\0');
                filenames[fileId] = name;
            }
            return filenames;
        }
    }
}
