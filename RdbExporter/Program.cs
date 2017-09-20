using Microsoft.Extensions.CommandLineUtils;
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
            var cmd = new CommandLineApplication();
            
            var listOption = cmd.Option("-l | --list", "List RDB Types",  CommandOptionType.NoValue);
            var pathOption = cmd.Option("-i | --installDir <SWLPath>", "Path to SWL installation",  CommandOptionType.SingleValue);
            var rdbOption = cmd.Option("-d | --rdb <RDBNumberOrName>", "The name or ID of the RDB type to export.",  CommandOptionType.SingleValue);
            var rawOption = cmd.Option("-r | --raw", "Export raw .dat files instead of content-aware.", CommandOptionType.NoValue);
            cmd.HelpOption("-? | -h | --help");

            cmd.OnExecute(() => {
                if (!pathOption.HasValue())
                {
                    Console.WriteLine("--installDir option is required.");
                    cmd.ShowHelp();
                    return 1;
                }

                if (listOption.HasValue())
                {
                    PrintIndex(pathOption.Value());
                    return 0;
                }
                
                if (!rdbOption.HasValue())
                {
                    Console.WriteLine("--rdb or --list option is required.");
                    cmd.ShowHelp();
                    return 1;
                }

                if (rawOption.HasValue())
                {
                    //TODO: Export just to raw .dat files
                    return 0;
                }

                //TODO: Genericize this more or something, maybe have the RDBTypes.json have a target method?
                if(rdbOption.Value() == "strings" || rdbOption.Value() == "1030002")
                {
                    ExportStrings(pathOption.Value());
                }
                else if (rdbOption.Value() == "flashImages" || rdbOption.Value() == "1000624")
                {

                }

                return 0;
            });

            cmd.Execute(args);
        }

        private static List<IDBRIndexEntrty> GetRdbIndex(string installDir)
        {
            var rdbPath = Path.Combine(installDir, "RDB");
            return IBDRParser.ParseIBDRFile(Path.Combine(rdbPath, "le.idx"));
        }

        private static void PrintIndex(string installDir)
        {
            Console.WriteLine("Index RDB Types:");
            var rdbTypes = GetRdbIndex(installDir);
            foreach(var rdbType in rdbTypes.Select(i => i.Type).Distinct())
            {
                //TODO firendly names
                Console.WriteLine($"Type ID: {rdbType}   Friendly Name: { rdbType }");
            }
        }

        static void ExportStrings(string installDir)
        {
            var indexEntries = GetRdbIndex(installDir);
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
