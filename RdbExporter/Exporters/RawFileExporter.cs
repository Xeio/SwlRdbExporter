using System;
using RdbExporter.Entities;
using System.Threading.Tasks;
using System.IO;
using RdbExporter.Utilities;

namespace RdbExporter.Exporters
{
    public class RawFileExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            //Parallel.ForEach(parameters.RdbFileEntries, entry => Process(parameters, entry));
            foreach(var entry in parameters.RdbFileEntries)
            {
                Process(parameters, entry);
            }
        }

        public void Process(ExportParameters parameters, IDBRIndexEntrty entry)
        {
            if (parameters.Arguments.Count < 0) throw new ArgumentException("RawFileExporter requires extension as first argument.");
            var outputPath = Path.ChangeExtension(Path.Combine(parameters.ExportDirectory, entry.Id.ToString()), parameters.Arguments[0]);
            using (var reader = entry.OpenEntryFile(parameters.SwlInstallDir))
            using (var writer = File.OpenWrite(outputPath))
            {
                reader.BaseStream.CopyToLimited(writer, entry.FileLength);
            }
        }
    }
}
