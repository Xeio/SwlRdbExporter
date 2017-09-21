using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdbExporter.Entities
{
    public class RdbType
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string ExporterType { get; set; }
        public List<string> ExporterArguments { get; set; } = new List<string>();
        /// <summary>
        /// Flag, indicates if the type is known to be used in SWL
        /// </summary>
        public bool IsSWL { get; set; }
    }
}
