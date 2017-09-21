using System;
using System.IO;
using System.Text;

namespace RdbExporter.Utilities
{
    public static class BinaryReaderExtensions
    {
        public static string ReadInt32PrefacedString(this BinaryReader reader, bool hasNullTerminatior = true)
        {
            var stringLength = reader.ReadInt32();
            var value = Encoding.ASCII.GetString(reader.ReadBytes(stringLength));
            if (hasNullTerminatior)
            {
                reader.ReadByte(); //Consume the null terminator on the string (not included as part of length)
            }
            return value;
        }

        public static void CopyToLimited(this Stream fromStream, Stream toStream, int bytes)
        {
            byte[] buffer = new byte[4096];
            int read;
            while (bytes > 0 && (read = fromStream.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                toStream.Write(buffer, 0, read);
                bytes -= read;
            }
        }
    }
}