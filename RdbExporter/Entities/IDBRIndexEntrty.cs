using System;
using System.IO;

namespace RdbExporter.Entities
{
    public class IDBRIndexEntrty
    {
        private int? _rawFileLength;

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

            var reader = new BinaryReader(File.Open(rdbFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            reader.BaseStream.Seek(FileOffset - 16, SeekOrigin.Begin);

            if (reader.ReadInt32() != Type) throw new Exception("Error opening RDB File. Type Mismatch.");
            if (reader.ReadInt32() != Id) throw new Exception("Error opening RDB File. ID Mismatch.");
            if (reader.ReadInt32() != (_rawFileLength ?? FileLength)) throw new Exception("Error opening RDB File. Length Mismatch.");
            var e = reader.ReadInt32(); //Not sure what this is

            //Some file types have an additional 12-byte header injected into the file data, which starts with the RDB Type. Check for that and skip if needed.
            var type = reader.ReadInt32();
            if(type == Type)
            {
                if (!_rawFileLength.HasValue)
                {
                    //Keep track of the raw file length, the public length should match the actual readable stream length for consumers of the stream.
                    _rawFileLength = FileLength;
                    FileLength = _rawFileLength.Value - 12;
                }
                reader.BaseStream.Seek(8, SeekOrigin.Current);
            }
            else
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
            }

            return reader;
        }
    }
}
