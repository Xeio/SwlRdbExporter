using System.Collections.Generic;
using System.Linq;
using RdbExporter.Entities;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace RdbExporter.Exporters
{
    public class MergedJpgExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            foreach(var rdbEntry in parameters.RdbFileEntries)
            {
                var bytes = rdbEntry.OpenEntryFile(parameters.SwlInstallDir).ReadBytes(rdbEntry.FileLength);
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
                //The first image is a 512x512 of the whole map
                //The next 16 are 512x512s of the map split into 16 pieces
                //Next 64 are... blah blah
                //The rest are a 16x16 we want to combine into a single image
                var highestResImages = streams.Skip(1).Skip(4 * 4).Skip(8 * 8).Select(s => Image.FromStream(s)).ToList();
                if (highestResImages.Count == 16 * 16)
                {
                    var widthHeight = highestResImages.First().Width;
                    var finalImage = new Bitmap(widthHeight * 16, widthHeight * 16);
                    using (var graphics = Graphics.FromImage(finalImage))
                    {
                        for (int y = 0; y < 16; y++)
                        {
                            for (int x = 0; x < 16; x++)
                            {
                                PointF ulCorner = new PointF(x * widthHeight, y * widthHeight);
                                PointF urCorner = new PointF(x * widthHeight + widthHeight, y * widthHeight);
                                PointF llCorner = new PointF(x * widthHeight, y * widthHeight + widthHeight);
                                PointF[] destPara = { ulCorner, urCorner, llCorner };
                                graphics.DrawImage(highestResImages[x + y * 16], destPara);
                            }
                        }
                        graphics.Save();
                    }
                    finalImage.Save(Path.Combine(parameters.ExportDirectory, $"{rdbEntry.Id}.png"), ImageFormat.Png);
                }
            }
        }
    }
}
