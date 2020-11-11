using RdbExporter.Entities;
using System.IO;
using System.Collections.Generic;
using RdbExporter.Parsers;
using RdbExporter.Utilities;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RdbExporter.Exporters
{
    public class StringExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            ExportStrings(parameters);
        }

        private void ExportStrings(ExportParameters parameters)
        {
            var indexEntries = Helpers.GetRdbIndex(parameters.SwlInstallDir);
            var stringEntries = indexEntries.Where(i => i.Type == 1030002).ToDictionary(i => i.Id);

            var languageFiles = Directory.EnumerateFiles(Path.Combine(parameters.SwlInstallDir, @"Data\Text\"), "*.tdbl", SearchOption.TopDirectoryOnly).Select(TDL1Parser.ParseTDL1File).ToList();

            var usedFiles = new HashSet<int>();
            foreach (var languageFile in languageFiles)
            {
                Parallel.ForEach(languageFile.Entries, (languageEntry) =>
                    WriteOutputJson(parameters, stringEntries[languageEntry.FileId], languageEntry, languageFile)
                );

                foreach (var fileid in languageFile.Entries.Select(e => e.FileId))
                {
                    //Track which files we used
                    usedFiles.Add(fileid);
                }
            }

            Parallel.ForEach(indexEntries.Where(i => i.Type == 1030002 && !usedFiles.Contains(i.Id)), (rdbEntry) =>
                    WriteOutputJson(parameters, rdbEntry, null, null)
                );
        }

        private void WriteOutputJson(ExportParameters parameters, IDBRIndexEntrty rdbIndexEntry, TDL1Entry languageEntry, TDL1File languageFile)
        {
            var tdc2File = TDC2Parser.ParseTDC2File(rdbIndexEntry.OpenEntryFile(parameters.SwlInstallDir));

            if (tdc2File.Entries.Count == 0) return;

            var settings = new JsonSerializerOptions() { IgnoreNullValues = true, WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var output = JsonSerializer.Serialize(tdc2File.Entries, settings);
            output = Regex.Replace(output, @"\\u[0-9A-F]{4}", " "); //Replace unicode endpoints, there are mostly variants of non-breaking spaces

            var outputPath = Path.Combine(parameters.ExportDirectory, languageFile?.LanguageName ?? "Unknown");
            Directory.CreateDirectory(outputPath);

            if (languageEntry != null)
            {
                outputPath = Path.Combine(outputPath, $"{languageEntry.RdbId}_{languageEntry.FriendlyName}");
            }
            else
            {
                outputPath = Path.Combine(outputPath, $"{tdc2File.Category}_{rdbIndexEntry.Id}");
            }
            outputPath = Path.ChangeExtension(outputPath, ".json");

            File.WriteAllText(outputPath, output);
        }
    }
}
