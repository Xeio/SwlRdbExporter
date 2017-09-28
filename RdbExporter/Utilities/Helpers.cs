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
        private static object _lockObject = new object();
        private static Lazy<List<RdbType>> _knownRdbTypes = new Lazy<List<RdbType>>(GetKnownRdbTypes, true);
        private static List<IDBRIndexEntrty> _rdbIndex;
        private static Dictionary<int, Dictionary<int, string>> _filenameIndex;

        public static List<IDBRIndexEntrty> GetRdbIndex(string installDir)
        {
            if (_rdbIndex != null) return _rdbIndex;

            lock (_lockObject)
            {
                if (_rdbIndex != null) return _rdbIndex;

                var rdbPath = Path.Combine(installDir, "RDB");
                return _rdbIndex = IBDRParser.ParseIBDRFile(Path.Combine(rdbPath, "le.idx"));
            }
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

        public static Dictionary<int, Dictionary<int, string>> GetFilenameIndex(string installDir)
        {
            if (_filenameIndex != null) return _filenameIndex;

            lock (_lockObject)
            {
                if (_filenameIndex != null) return _filenameIndex;

                var nameIndexRdb = GetRdbIndex(installDir).FirstOrDefault(i => i.Id == 1 && i.Type == 1000010);
                if (nameIndexRdb != null)
                {
                    return _filenameIndex = FilenameIndexParser.ParseNameIndex(nameIndexRdb.OpenEntryFile(installDir));
                }
                return _filenameIndex = new Dictionary<int, Dictionary<int, string>>();
            }
        }
    }
}
