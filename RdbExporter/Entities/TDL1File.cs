using System.Collections.Generic;

namespace RdbExporter.Entities
{
    public class TDL1File
    { 
        public string LanguageCode { get; set; }
        public string LanguageName { get; set; }
        public string LanguageNameInLanguage { get; set; }
        public List<TDL1Entry> Entries { get; set; } = new List<TDL1Entry>();
    }
}
