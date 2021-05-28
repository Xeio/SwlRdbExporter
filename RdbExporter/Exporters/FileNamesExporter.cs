using RdbExporter.Entities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace RdbExporter.Exporters
{
    public class FileNamesExporter : IExporter
    {
        private record NameIndex(int FileId, string name);

        public void RunExport(ExportParameters parameters)
        {
            using var stream = parameters.RdbFileEntries.First().OpenEntryFile(parameters.SwlInstallDir);

            var totalRdbIds = stream.ReadInt32();
            for (var j = 0; j < totalRdbIds; j++)
            {
                List<NameIndex> index = new();

                var rdbId = stream.ReadInt32();
                var count = stream.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var fileId = stream.ReadInt32();
                    var fileNameLength = stream.ReadInt32();
                    var name = stream.ReadBytes(fileNameLength);
                    index.Add(new NameIndex(fileId, Encoding.ASCII.GetString(name).TrimEnd('\0')));
                }

                var settings = new JsonSerializerOptions() { IgnoreNullValues = true, WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                var output = JsonSerializer.Serialize(index, settings);
                File.WriteAllText(Path.Combine(parameters.ExportDirectory, $"RDB-{rdbId}.json"), output);
            }
        }
    }
}
