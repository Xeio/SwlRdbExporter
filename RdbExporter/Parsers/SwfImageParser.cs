using RdbExporter.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace RdbExporter.Parsers
{
    public class SwfImageParser
    {
        static readonly SwfTagCode[] IMAGE_CODES = { SwfTagCode.DefineBitsLossless, SwfTagCode.DefineBitsLossless2, SwfTagCode.DefineBitsJpeg2, SwfTagCode.DefineBitsJpeg3, SwfTagCode.DefineBitsJpeg4 };

        public static IEnumerable<Image> ParseImagesFromSwfFile(string file)
        {
            return ParseImagesFromSwfFile(File.OpenRead(file));
        }

        public static IEnumerable<Image> ParseImagesFromSwfFile(Stream stream)
        {
            using (stream)
            {
                using var reader = new BittableBinaryReader(stream);

                var header = Encoding.ASCII.GetString(reader.ReadBytes(3));
                if (header == "ZWS") throw new InvalidOperationException($"Unspported file type '{header}'.");
                else if (header != "CWS" && header != "FWS")
                {
                    throw new InvalidDataException("Not a valid SWF file.");
                }

                var versionNumber = reader.ReadByte();
                var fileLength = reader.ReadInt32(); //This is the uncompressed length, so won't match file size for CWF

                if (header == "CWS")
                {
                    //CWS Files are compressed with Zlib
                    reader.ReadBytes(2); //Skip the zlib header, since DeflateStream won't process it correctly
                    using var compressedReader = new BittableBinaryReader(new DeflateStream(reader.BaseStream, CompressionMode.Decompress));
                    return ReadHeaderAndImages(compressedReader);
                }
                return ReadHeaderAndImages(reader);
            }
        }
        private static IEnumerable<Image> ReadHeaderAndImages(BittableBinaryReader reader)
        {
            ReadRect(reader);
            var framerate = reader.ReadInt16();
            var frameCount = reader.ReadInt16();

            var tags = ReadTags(reader);

            return tags.Where(t => IMAGE_CODES.Contains(t.Code)).Select(t => HandleImageTag(t));
        }

        private static void ReadRect(BittableBinaryReader reader)
        {
            //This is the "twip" sized boundaries of the SWF
            int length = reader.ReadBits(5); //Bit length of bounds
            _ = reader.ReadBits(length); //xMin
            _ = reader.ReadBits(length); //xMax
            _ = reader.ReadBits(length); //yMin
            _ = reader.ReadBits(length); //yMax
        }

        private static List<SwfTag> ReadTags(BittableBinaryReader reader)
        {
            var tags = new List<SwfTag>();
            do
            {
                tags.Add(ReadTag(reader));
            } while (tags[tags.Count - 1].Code != SwfTagCode.End);
            return tags;
        }

        private static SwfTag ReadTag(BittableBinaryReader reader)
        {
            uint tagCodeAndLength = reader.ReadUInt16();
            uint code = (tagCodeAndLength & 0b1111_1111_1100_0000) >> 6; //upper 10 bits are code
            uint length = tagCodeAndLength & 0b0011_1111; //lower 6 bits are length
            if (length == 0b0011_1111)
            {
                //If all length bits are set, this is a large block and uses a UInt to signal size
                length = reader.ReadUInt32();
            }
            var data = reader.ReadBytes((int)length); //This cast isn't safe, probably should check this
            return new SwfTag() { Code = (SwfTagCode)code, Data = data };
        }

        private static Image HandleImageTag(SwfTag tag)
        {
            return tag.Code switch
            {
                SwfTagCode.DefineBitsLossless => HandleLossless(tag, false),
                SwfTagCode.DefineBitsLossless2 => HandleLossless(tag, true),
                SwfTagCode.DefineBitsJpeg3 => HandleJpeg3(tag),
                SwfTagCode.DefineBitsJpeg2 => HandleJpeg2(tag),
                _ => throw new Exception($"Didn't handle code type {tag.Code}"),
            };
        }

        private static Image HandleLossless(SwfTag tag, bool alpha)
        {
            var binaryReader = new BinaryReader(new MemoryStream(tag.Data));
            var characterId = binaryReader.ReadUInt16();
            var format = binaryReader.ReadByte();
            var width = binaryReader.ReadUInt16();
            var height = binaryReader.ReadUInt16();
            int[] colorTable = null;
            if (format == 3)
            {
                colorTable = new int[binaryReader.ReadByte() + 1]; //Color table size
            }
            if (format != 5 && format != 3) throw new Exception($"Unsupported Lossless format '{format}'");

            binaryReader.ReadBytes(2); //Eat the Zlib header
            var decompressedData = ReadAll(new DeflateStream(binaryReader.BaseStream, CompressionMode.Decompress));

            int dataIndex = 0;
            if(format == 3)
            {
                for(int i = 0; i < colorTable.Length; i++)
                {
                    if (alpha)
                    {
                        //32 bit RGBA
                        colorTable[i] = Color.FromArgb(decompressedData[dataIndex + 3], decompressedData[dataIndex + 0], decompressedData[dataIndex + 1], decompressedData[dataIndex + 2]).ToArgb();
                        dataIndex += 4; 
                    }
                    else
                    {
                        //24 bit RGB
                        colorTable[i] = Color.FromArgb(255, decompressedData[dataIndex + 0], decompressedData[dataIndex + 1], decompressedData[dataIndex + 2]).ToArgb();
                        dataIndex += 3;
                    }
                }
            }

            var bitmap = new Bitmap(width, height);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
            int imageIndex = 0;
            unsafe
            {
                Span<int> imagePointer = new Span<int>(bitmapData.Scan0.ToPointer(), width * height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (format == 5)
                        {
                            if (alpha)
                            {
                                //32 bit RGBA
                                imagePointer[imageIndex] = Color.FromArgb(decompressedData[dataIndex], decompressedData[dataIndex + 1], decompressedData[dataIndex + 2], decompressedData[dataIndex + 3]).ToArgb();
                                dataIndex += 4;
                            }
                            else
                            {
                                //32 bits, but the first 8 bits should be ignored, rest is RGB
                                imagePointer[imageIndex] = Color.FromArgb(255, decompressedData[dataIndex + 1], decompressedData[dataIndex + 2], decompressedData[dataIndex + 3]).ToArgb();
                                dataIndex += 4;
                            }
                        }
                        else if (format == 3)
                        {
                            //8 bit, pointer to color table
                            imagePointer[imageIndex] = colorTable[decompressedData[dataIndex]];
                            dataIndex += 1;
                        }
                        imageIndex += 1;
                    }
                    
                    //Scan lines are aligned to 32-bit lengths, check if we need to adjust for padding
                    if (format == 3 && width % 4 > 0)
                    {
                        dataIndex += 4 - width % 4;
                    }
                }
            }
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private static Image HandleJpeg3(SwfTag tag)
        {
            using var binaryReader = new BinaryReader(new MemoryStream(tag.Data, 0, 6));
            _ = binaryReader.ReadUInt16(); //characterId
            int alphaDataOffset = (int)binaryReader.ReadUInt32(); //Not a safe cast probably
            return Image.FromStream(new MemoryStream(tag.Data, 6, alphaDataOffset));
        }

        private static Image HandleJpeg2(SwfTag tag)
        {
            using var binaryReader = new BinaryReader(new MemoryStream(tag.Data, 0, 2));
            _ = binaryReader.ReadUInt16(); //characterId
            return Image.FromStream(new MemoryStream(tag.Data, 2, tag.Data.Length - 2));
        }

        private static byte[] ReadAll(Stream stream)
        {
            using (stream)
            using (var memoryStream = new MemoryStream(1))
            {
                stream.CopyTo(memoryStream);
                return memoryStream.GetBuffer();
            }
        }
    }

    public class SwfTag
    {
        public SwfTagCode Code { get; set; }
        public byte[] Data { get; set; }
    }

    public enum SwfTagCode : uint
    {
        /// <summary>
        /// End tag, always the last tag in the file
        /// </summary>
        End = 0,
        ShowFrame = 1,
        DefineShape = 2,
        PlaceObject = 4,
        RemoveObject = 5,
        DefineBits = 6,
        JpegTables = 8,
        SetBackgroundColor = 9,
        DefineText = 11,
        DoAction = 12,
        DefineBitsLossless = 20,
        DefineBitsJpeg2 = 21,
        DefineShape2 = 22,
        Protect = 24,
        PlaceObject2 = 26,
        RemoveObject2 = 28,
        DefineShape3 = 32,
        DefineBitsJpeg3 = 35,
        DefineBitsLossless2 = 36,
        DefineEditText = 37,
        FrameLabel = 43,
        /// <summary>
        /// This will always be first tag in SWF 8 and later
        /// </summary>
        FileAttributes = 69,
        ImportAssets = 71,
        DefineFontAlignZones = 73,
        CsmTextSettings = 74,
        DefineFont3 = 75,
        SymbolClass = 76,
        Metadata = 77,
        DefineScalingGrid = 78,
        DefineSceneAndFrameLabelData = 86,
        DefineFontName = 88,
        DefineBitsJpeg4 = 90
    }

}