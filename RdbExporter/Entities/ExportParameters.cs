using System.Collections.Generic;

namespace RdbExporter.Entities
{
    public class ExportParameters
    {
        public RdbType RdbType {get;set;}
        public string SwlInstallDir { get; set; }
        public List<IDBRIndexEntrty> RdbFileEntries { get; set; } = new List<IDBRIndexEntrty>();
        public string ExportDirectory { get; set; }
        public List<string> Arguments { get; set; } = new List<string>();
    }
}
