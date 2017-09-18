using System;
using System.IO;

namespace RdbExporter.Entities
{
    public class IDBRIndexEntrty
    {
        public int Type { get; set; }
        public int Id { get; set; }
        public int FileNumber { get; set; }
        public int FileOffset { get; set; }
        public int FileLength { get; set; }

        public BinaryReader OpenEntryFile(string installDir)
        {
            var rdbPath = Path.Combine(installDir, "RDB");

            var rdbFile = Path.Combine(rdbPath, FileNumber.ToString("00"));
            rdbFile = Path.ChangeExtension(rdbFile, ".rdbdata");

            var reader = new BinaryReader(File.OpenRead(rdbFile));
            reader.BaseStream.Seek(FileOffset, SeekOrigin.Begin);

            if (reader.ReadInt32() != Type) throw new Exception("Error opening RDB File. Type Mismatch.");
            if (reader.ReadInt32() != Id) throw new Exception("Error opening RDB File. ID Mismatch.");

            reader.ReadBytes(4); //Not sure what this is 1,000,002 int value?

            return reader;
        }
    }
}
