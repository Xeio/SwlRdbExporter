using Newtonsoft.Json;
using RdbExporter.Entities;
using RdbExporter.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RdbExporter.Utilities
{
    public class Helpers
    {
        private static Lazy<List<RdbType>> _knownRdbTypes = new Lazy<List<RdbType>>(GetKnownRdbTypes, true);

        public static List<IDBRIndexEntrty> GetRdbIndex(string installDir)
        {
            var rdbPath = Path.Combine(installDir, "RDB");
            return IBDRParser.ParseIBDRFile(Path.Combine(rdbPath, "le.idx"));
        }

        private static List<RdbType> GetKnownRdbTypes()
        {
            var rdbTypesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "RdbTypes.json");
            return JsonConvert.DeserializeObject<List<RdbType>>(File.ReadAllText(rdbTypesFile));
        }

        public static RdbType GetKnownRdbType(string nameOrId)
        {
            if (int.TryParse(nameOrId, out int id))
            {
                return _knownRdbTypes.Value.FirstOrDefault(r => r.Id == id);
            }
            return _knownRdbTypes.Value.FirstOrDefault(r => string.Equals(r.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
