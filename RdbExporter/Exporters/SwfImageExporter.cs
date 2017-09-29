using System.Threading.Tasks;
using RdbExporter.Entities;
using System.Drawing.Imaging;
using RdbExporter.Parsers;
using System.IO;
using System;
using System.Diagnostics;

namespace RdbExporter.Exporters
{
    public class SwfImageExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            Parallel.ForEach(parameters.RdbFileEntries, rdbEntry =>
            {
                try
                {
                    var i = 0;
                    foreach (var image in SwfImageParser.ParseImagesFromSwfFile(rdbEntry.OpenEntryFile(parameters.SwlInstallDir).BaseStream))
                    {
                        string filename = rdbEntry.Id + (i++ > 0 ? "-" + i : "") + ".png";
                        image.Save(Path.Combine(parameters.ExportDirectory, filename), ImageFormat.Png);
                        image.Dispose();
                    }
                }
                catch (ArgumentException)
                {
                    Debug.WriteLine($"Failed to parse images from RDB ID '{rdbEntry.Id}'");
                }
                catch (InvalidDataException)
                {
                    Debug.WriteLine($"RDB ID '{rdbEntry.Id}' is not a vaild SWF File.");
                }
            });
        }
    }
}
