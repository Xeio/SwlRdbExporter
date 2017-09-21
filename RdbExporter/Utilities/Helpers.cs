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
        public static List<IDBRIndexEntrty> GetRdbIndex(string installDir)
        {
            var rdbPath = Path.Combine(installDir, "RDB");
            return IBDRParser.ParseIBDRFile(Path.Combine(rdbPath, "le.idx"));
        }

        public static RdbType GetRdbType(string nameOrId)
        {
            var rdbTypesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "RdbTypes.json");
            var rdbTypes = JsonConvert.DeserializeObject<List<RdbType>>(File.ReadAllText(rdbTypesFile));
            if (int.TryParse(nameOrId, out int id))
            {
                return rdbTypes.FirstOrDefault(r => r.Id == id);
            }
            return rdbTypes.FirstOrDefault(r => string.Equals(r.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
