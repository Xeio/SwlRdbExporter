﻿using System;
using RdbExporter.Entities;
using System.IO;
using RdbExporter.Utilities;
using System.Threading.Tasks;

namespace RdbExporter.Exporters
{
    public class RawFileExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            if (parameters.Arguments.Count < 1) throw new ArgumentException("RawFileExporter requires extension as first argument.");

            Parallel.ForEach(parameters.RdbFileEntries, (rdbEntry) => Process(parameters, rdbEntry));
        }

        public void Process(ExportParameters parameters, IDBRIndexEntrty entry)
        {
            string outputPath;
            var filenameIndex = Helpers.GetFilenameIndex(parameters.SwlInstallDir);
            if(filenameIndex.TryGetValue(entry.Type, out var idToNameDictionary) && idToNameDictionary.TryGetValue(entry.Id, out var filename))
            {
                //The file RDB Type and File ID exist in the name index, so lets use the name from there
                outputPath = Path.Combine(parameters.ExportDirectory, filename);
            }
            else
            {
                 outputPath = Path.ChangeExtension(Path.Combine(parameters.ExportDirectory, entry.Id.ToString()), parameters.Arguments[0]);
            }

            using var reader = entry.OpenEntryFile(parameters.SwlInstallDir);
            using var writer = File.OpenWrite(outputPath);
            reader.BaseStream.CopyToLimited(writer, entry.FileLength);
        }
    }
}
