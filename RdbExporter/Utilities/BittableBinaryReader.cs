using System.IO;
using System.Text;

namespace RdbExporter.Utilities
{
    public class BittableBinaryReader : BinaryReader
    {
        private int BitIndex { get; set; } = -1;
        private byte CurrentByte { get; set; }

        public BittableBinaryReader(Stream input) : base(input) { }
        public BittableBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }
        public BittableBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public int ReadBits(int count)
        {
            int output = 0;
            for (int i = 0; i < count; i++)
            {
                output <<= 1;
                output |= ReadBit();
            }
            return output;
        }

        public int ReadBit()
        {
            if (BitIndex < 0)
            {
                CurrentByte = ReadByte();
                BitIndex = 7;
            }
            return (CurrentByte & (1 << BitIndex--)) > 0 ? 1 : 0;
        }

        public void ResetBits()
        {
            BitIndex = -1;
        }
    }
}
