using Microsoft.Extensions.CommandLineUtils;
using RdbExporter.Entities;
using RdbExporter.Exporters;
using RdbExporter.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

                var rdbType = Helpers.GetRdbType(rdbOption.Value());
                if (rdbType == null)
                {
                    if(int.TryParse(rdbOption.Value(), out int rdbId))
                    {
                        Console.WriteLine($"Known RdbType with name or ID '{rdbOption.Value()}' not found. Exporting as raw .dat files.");
                        rdbType = new RdbType() { Id = rdbId };
                    }
                    else
                    {
                        Console.Error.WriteLine("RdbType provided is not a know named type and is an invalid number.");
                        return 1;
                    }
                }

                var rdbIndex = Helpers.GetRdbIndex(pathOption.Value());
                var exportParameters = new ExportParameters()
                {
                    RdbType = rdbType,
                    Arguments = rdbType.ExporterArguments,
                    SwlInstallDir = pathOption.Value(),
                    RdbFileEntries = rdbIndex.Where(i => i.Type == rdbType.Id).ToList(),
                    ExportDirectory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), string.IsNullOrWhiteSpace(rdbType.Name) ? rdbType.Id.ToString() : $"{rdbType.Id} - {rdbType.Name}")).FullName
                };
                
                if (exportParameters.RdbFileEntries.Count == 0)
                {
                    Console.Error.WriteLine($"No entries found for RDB Type Id '{rdbType.Id}'.");
                    return 1;
                }

                if (!string.IsNullOrWhiteSpace(rdbType.ExporterType) && !rawOption.HasValue())
                {
                    var type = Type.GetType(rdbType.ExporterType);
                    var exporter = Activator.CreateInstance(type) as IExporter;
                    exporter.RunExport(exportParameters);
                }
                else
                {
                    Console.WriteLine("Exporting as raw .dat files.");
                    exportParameters.Arguments = new List<string>{ ".dat" };
                    new RawFileExporter().RunExport(exportParameters);
                }

                return 0;
            });

            cmd.Execute(args);
        }        

        private static void PrintIndex(string installDir)
        {
            Console.WriteLine("Index RDB Types:");
            var rdbTypes = Helpers.GetRdbIndex(installDir);
            foreach(var rdbType in rdbTypes.Select(i => i.Type).Distinct())
            {
                //TODO firendly names
                Console.WriteLine($"Type ID: {rdbType}   Friendly Name: { rdbType }");
            }
        }

        
    }
}
