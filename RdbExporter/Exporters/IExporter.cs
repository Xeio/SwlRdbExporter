using RdbExporter.Entities;

namespace RdbExporter.Exporters
{
    public interface IExporter
    {
        void RunExport(ExportParameters parameters);
    }
}
