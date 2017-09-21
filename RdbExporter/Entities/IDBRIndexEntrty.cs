using System;
using System.IO;
using System.Linq;

namespace RdbExporter.Entities
{
    public class IDBRIndexEntrty
    {
        public int Type { get; set; }
        public int Id { get; set; }
        public int FileNumber { get; set; }
        public int FileOffset { get; set; }
        public int FileLength { get; set; }

        public BinaryReader OpenEntryFile(string installDir, int skipBytes)
        {
            var rdbPath = Path.Combine(installDir, "RDB");

            var rdbFile = Path.Combine(rdbPath, FileNumber.ToString("00"));
            rdbFile = Path.ChangeExtension(rdbFile, ".rdbdata");

            var reader = new BinaryReader(File.Open(rdbFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            reader.BaseStream.Seek(FileOffset - 16, SeekOrigin.Begin);

            if (reader.ReadInt32() != Type) throw new Exception("Error opening RDB File. Type Mismatch.");
            if (reader.ReadInt32() != Id) throw new Exception("Error opening RDB File. ID Mismatch.");
            if (reader.ReadInt32() != FileLength) throw new Exception("Error opening RDB File. Length Mismatch.");
            reader.ReadInt32(); //Not sure what this is

            //Some file types seem to have data in front of the file itself, notable the TDC1 files have a unknown 12 byte header (it's close, but not quite identical to all of them)
            reader.ReadBytes(skipBytes);

            return reader;
        }
    }
}
