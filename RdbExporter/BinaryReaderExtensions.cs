using System.IO;
using System.Text;

namespace RdbExporter
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
    }
}
