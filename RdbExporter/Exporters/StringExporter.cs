using System;
using RdbExporter.Entities;
using System.IO;
using System.Collections.Generic;
using RdbExporter.Parsers;
using RdbExporter.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
                //Parallel.ForEach(languageFile.Entries, (languageEntry) =>
                //    WriteOutputJson(installDir, stringEntries[languageEntry.FileId], languageEntry, languageFile)
                //);
                foreach (var languageEntry in languageFile.Entries) WriteOutputJson(parameters, stringEntries[languageEntry.FileId], languageEntry, languageFile);

                foreach (var fileid in languageFile.Entries.Select(e => e.FileId))
                {
                    //Track which files we used
                    usedFiles.Add(fileid);
                }
            }

            //Parallel.ForEach(indexEntries.Where(i => i.Type == 1030002 && !usedFiles.Contains(i.Id)), (rdbEntry) =>
            //        WriteOutputJson(installDir, rdbEntry)
            //    );

            foreach (var rdbEntry in indexEntries.Where(i => i.Type == 1030002 && !usedFiles.Contains(i.Id))) WriteOutputJson(parameters, rdbEntry);
        }

        private void WriteOutputJson(ExportParameters parameters, IDBRIndexEntrty rdbIndexEntry, TDL1Entry languageEntry, TDL1File languageFile)
        {
            var tdc2File = TDC2Parser.ParseTDC2File(rdbIndexEntry.OpenEntryFile(parameters.SwlInstallDir, parameters.RdbType.SkipBytes));

            if (tdc2File.Entries.Count == 0) return;

            var settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented };
            var output = JsonConvert.SerializeObject(tdc2File.Entries, settings);

            var outputPath = Path.Combine(parameters.ExportDirectory, languageFile.LanguageName);
            Directory.CreateDirectory(outputPath);

            outputPath = Path.Combine(outputPath, $"{languageEntry.RdbId}_{languageEntry.FriendlyName}");
            outputPath = Path.ChangeExtension(outputPath, ".json");

            File.WriteAllText(outputPath, output);
        }

        private void WriteOutputJson(ExportParameters parameters, IDBRIndexEntrty rdbIndexEntry)
        {
            var tdc2File = TDC2Parser.ParseTDC2File(rdbIndexEntry.OpenEntryFile(parameters.SwlInstallDir, parameters.RdbType.SkipBytes));

            if (tdc2File.Entries.Count == 0) return;

            var settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented };
            var output = JsonConvert.SerializeObject(tdc2File.Entries, settings);

            var outputPath = Path.Combine(parameters.ExportDirectory, "Unknown");
            Directory.CreateDirectory(outputPath);

            outputPath = Path.Combine(outputPath, $"{tdc2File.Category}_{rdbIndexEntry.Id}");
            outputPath = Path.ChangeExtension(outputPath, ".json");

            File.WriteAllText(outputPath, output);
        }
    }
}
