using System.Threading.Tasks;
using RdbExporter.Entities;
using System.Drawing.Imaging;
using RdbExporter.Parsers;
using System.IO;

namespace RdbExporter.Exporters
{
    public class SwfImageExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            Parallel.ForEach(parameters.RdbFileEntries, rdbEntry =>
            {
                var i = 0;
                foreach (var image in SwfImageParser.ParseImagesFromSwfFile(rdbEntry.OpenEntryFile(parameters.SwlInstallDir).BaseStream))
                {
                    string filename = rdbEntry.Id + (i++ > 0 ? "-" + i : "") + ".png";
                    image.Save(Path.Combine(parameters.ExportDirectory, filename), ImageFormat.Png);
                }
            });
        }
    }
}
