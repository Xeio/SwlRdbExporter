using System.Collections.Generic;
using System.Linq;
using RdbExporter.Entities;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.Threading.Tasks;

namespace RdbExporter.Exporters
{
    public class MergedJpgExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            Parallel.ForEach(parameters.RdbFileEntries, (rdbEntry) => Process(parameters, rdbEntry));
        }

        private static void Process(ExportParameters parameters, IDBRIndexEntrty rdbEntry)
        {
            using var file = rdbEntry.OpenEntryFile(parameters.SwlInstallDir);
            var bytes = file.ReadBytes(rdbEntry.FileLength);
            var streams = new List<MemoryStream>();
            int nextStartIndex = 0;
            for (int i = 1; i < bytes.Length; i++)
            {
                if (bytes[i] == 0xFF && bytes[i + 1] == 0xD9)
                {
                    streams.Add(new MemoryStream(bytes, nextStartIndex, i - nextStartIndex + 2));
                    nextStartIndex = i + 2;
                }
            }
            //The first image in the file is always the whole map as a single small image
            //After that, the map is split into N segments
            //Layouts such as 4x4, 8x8, 16x16, 3x3, 6x6, 12x12, 24x24
            //Also non-square layouts like 2x3, 4x6, 8x12, 16x32
            Image finalImage;
            if(streams.Count == 1)
            {
                finalImage = Image.FromStream(streams.First());
            }
            else
            {
                int widthInImageNumbers = 0, heightInImageNumbers = 0;
                if (streams.Count == 1 + 4 * 4)
                {
                    widthInImageNumbers = heightInImageNumbers = 4;
                    streams = streams.Skip(1).ToList();
                }
                else if (streams.Count == 1 + 4 * 4 + 8 * 8)
                {
                    widthInImageNumbers = heightInImageNumbers = 8;
                    streams = streams.Skip(1).Skip(4 * 4).ToList();
                }
                else if(streams.Count == 1 + 4 * 4 + 8 * 8 + 16 * 16)
                {
                    widthInImageNumbers = heightInImageNumbers = 16;
                    streams = streams.Skip(1).Skip(4 * 4).Skip(8 * 8).ToList();
                }
                //3x3, 6x6, 12x12, 24x24....
                else if (streams.Count == 1 + 3 * 3)
                {
                    widthInImageNumbers = heightInImageNumbers = 3;
                    streams = streams.Skip(1).ToList();
                }
                else if (streams.Count == 1 + 3 * 3 + 6 * 6)
                {
                    widthInImageNumbers = heightInImageNumbers = 6;
                    streams = streams.Skip(1).Skip(3 * 3).ToList();
                }
                else if (streams.Count == 1 + 3 * 3 + 6 * 6 + 12 * 12)
                {
                    widthInImageNumbers = heightInImageNumbers = 12;
                    streams = streams.Skip(1).Skip(3 * 3).Skip(6 * 6).ToList();
                }
                else if (streams.Count == 1 + 3 * 3 + 6 * 6 + 12 * 12 + 24 * 24)
                {
                    widthInImageNumbers = heightInImageNumbers = 24;
                    streams = streams.Skip(1).Skip(3 * 3).Skip(6 * 6).Skip(12 * 12).ToList();
                }
                else if (streams.Count == 1 + 2 * 3 + 4 * 6 + 8 * 12 + 16 * 24)
                {
                    //Non-square map
                    //2x3, 4x12, 8x12, 16x24
                    widthInImageNumbers = 16;
                    heightInImageNumbers = 24;
                    streams = streams.Skip(1).Skip(2 * 3).Skip(4 * 6).Skip(8 * 12).ToList();
                }
                else
                {
#if DEBUG
                    throw new Exception("Couldn't merge images, not a square number.");
#else
                    Console.WriteLine($"Unable to process file '{0}', couldn't determine grid layout. Found {streams.Count} images.");
#endif
                }

                //Some overlap in the images, account for that when merging with an offset
                const int OFFSET = 7;
                int width = 0, height = 0;
                using (Image image = Image.FromStream(streams[0]))
                {
                    width = image.Width - OFFSET;
                    height = image.Height - OFFSET;
                    finalImage = new Bitmap(width * widthInImageNumbers, height * heightInImageNumbers);
                }
                using var graphics = Graphics.FromImage(finalImage);
                for (int y = 0; y < heightInImageNumbers; y++)
                {
                    for (int x = 0; x < widthInImageNumbers; x++)
                    {
                        using Image image = Image.FromStream(streams[x + y * widthInImageNumbers]);
                        PointF ulCorner = new PointF(x * width, y * height);
                        PointF urCorner = new PointF(x * width + width + OFFSET, y * height);
                        PointF llCorner = new PointF(x * width, y * height + height + OFFSET);
                        PointF[] destPara = { ulCorner, urCorner, llCorner };
                        graphics.DrawImage(image, destPara);
                    }
                }
                graphics.Save();
            }

            if(finalImage != null)
            {
                finalImage.Save(Path.Combine(parameters.ExportDirectory, $"{rdbEntry.Id}.jpg"), ImageFormat.Jpeg);
            }
        }
    }
}
