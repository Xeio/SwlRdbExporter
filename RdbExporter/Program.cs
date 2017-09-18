using Newtonsoft.Json;
using RdbExporter.Entities;
using RdbExporter.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RdbExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1) throw new ArgumentException("Require install dir parameter");
            string installDir = args[0];
            //string exportDir = args?[1];
            //if (string.IsNullOrWhiteSpace(exportDir)) throw new ArgumentException("Require RDB 1030002 export dir parameter");

            ExportStrings(installDir);

            //var languageFiles = Directory.EnumerateFiles(Path.Combine(installDir, @"Data\Text\"), "*.tdbl", SearchOption.TopDirectoryOnly).Select(TDL1Parser.ParseTDL1File).ToList();
            ////var languageFiles = Directory.EnumerateFiles(@"C:\Users\joshu\Downloads\exported\1030001 (Language Index)", "*.dat", SearchOption.TopDirectoryOnly).Select(TDL1Parser.ParseTDL1File).ToList();

            //foreach(var languageFile in languageFiles)
            //{
            //    //Parallel.ForEach(languageFile.Entries, (entry) => WriteOutputJson(exportDir, entry, languageFile));
            //    Parallel.ForEach(languageFile.Entries, (entry) => WriteOutputJson(exportDir, entry, languageFile));
            //}
        }

        static void ExportStrings(string installDir)
        {
            var rdbPath = Path.Combine(installDir, "RDB");
            var indexEntries = IBDRParser.ParseIBDRFile(Path.Combine(rdbPath, "le.idx"));
            var stringEntries = indexEntries.Where(i => i.Type == 1030002).ToDictionary(i => i.Id);

            var languageFiles = Directory.EnumerateFiles(Path.Combine(installDir, @"Data\Text\"), "*.tdbl", SearchOption.TopDirectoryOnly).Select(TDL1Parser.ParseTDL1File).ToList();

            var usedFiles = new HashSet<int>();
            foreach (var languageFile in languageFiles)
            {
                Parallel.ForEach(languageFile.Entries, (languageEntry) =>
                    WriteOutputJson(installDir, stringEntries[languageEntry.FileId], languageEntry, languageFile)
                );

                foreach(var fileid in languageFile.Entries.Select(e => e.FileId))
                {
                    //Track which files we used
                    usedFiles.Add(fileid);
                }
            }

            Parallel.ForEach(indexEntries.Where(i => i.Type == 1030002 && !usedFiles.Contains(i.Id)), (rdbEntry) =>
                    WriteOutputJson(installDir, rdbEntry)
                );
        }

        static void WriteOutputJson(string installDir, IDBRIndexEntrty rdbIndexEntry, TDL1Entry languageEntry, TDL1File languageFile)
        {
            var tdc2File = TDC2Parser.ParseTDC2File(rdbIndexEntry.OpenEntryFile(installDir));

            if (tdc2File.Entries.Count == 0) return;

            var settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented };
            var output = JsonConvert.SerializeObject(tdc2File.Entries, settings);

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Json");
            outputPath = Path.Combine(outputPath, languageFile.LanguageName);
            Directory.CreateDirectory(outputPath);

            outputPath = Path.Combine(outputPath, $"{languageEntry.RdbId}_{languageEntry.FriendlyName}");
            outputPath = Path.ChangeExtension(outputPath, ".json");

            File.WriteAllText(outputPath, output);
        }

        static void WriteOutputJson(string installDir, IDBRIndexEntrty rdbIndexEntry)
        {
            var tdc2File = TDC2Parser.ParseTDC2File(rdbIndexEntry.OpenEntryFile(installDir));

            if (tdc2File.Entries.Count == 0) return;

            var settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented };
            var output = JsonConvert.SerializeObject(tdc2File.Entries, settings);

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Json");
            outputPath = Path.Combine(outputPath, "Unknown");
            Directory.CreateDirectory(outputPath);

            outputPath = Path.Combine(outputPath, $"{tdc2File.Category}_{rdbIndexEntry.Id}");
            outputPath = Path.ChangeExtension(outputPath, ".json");

            File.WriteAllText(outputPath, output);
        }
    }
}
