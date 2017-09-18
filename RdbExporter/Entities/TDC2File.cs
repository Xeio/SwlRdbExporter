using System.Collections.Generic;

namespace RdbExporter.Entities
{
    public class TDC2File
    {
        public int Category { get; set; }
        public List<TDC2Entry> Entries { get; set; } = new List<TDC2Entry>();
    }

    public class TDC2Entry
    {
        public int? ID { get; set; }
        public string Value { get; set; }
    }
}
