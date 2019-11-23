using RdbExporter.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RdbExporter.Exporters
{
    public class VoiceoverExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            Parallel.ForEach(parameters.RdbFileEntries, entry => Process(parameters, entry));
        }

        public void Process(ExportParameters parameters, IDBRIndexEntrty entry)
        {
            using var reader = entry.OpenEntryFile(parameters.SwlInstallDir);
            ReadOnlySpan<byte> fileContent = reader.ReadBytes(entry.FileLength);
            ReadOnlySpan<byte> oggHeader = new byte[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };
            var oggIndex = fileContent.IndexOf(oggHeader);
            if(oggIndex >= 0)
            {
                string outputPath = Path.ChangeExtension(Path.Combine(parameters.ExportDirectory, entry.Id.ToString()), ".ogg");
                File.WriteAllBytes(outputPath, fileContent.Slice(oggIndex).ToArray());
            }
        }
    }
}
