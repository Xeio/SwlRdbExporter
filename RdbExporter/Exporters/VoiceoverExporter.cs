using RdbExporter.Entities;
using RdbExporter.Utilities;
using System.IO;
using System.Threading.Tasks;

namespace RdbExporter.Exporters
{
    public class VoiceoverExporter : IExporter
    {
        public void RunExport(ExportParameters parameters)
        {
            Parallel.ForEach(parameters.RdbFileEntries, entry => Process(parameters, entry));
        }

        public void Process(ExportParameters parameters, IDBRIndexEntrty entry)
        {
            using (var reader = entry.OpenEntryFile(parameters.SwlInstallDir))
            {
                for(int i = 0; i < entry.FileLength; i++)
                {
                    if(reader.ReadByte() == 'O')
                    {
                        //Search for Ogg file header (maybe a better way to do this? Need to understand the LIP file type better)
                        if(reader.ReadByte() == 'g')
                        {
                            if (reader.ReadByte() == 'g')
                            {
                                if (reader.ReadByte() == 'S')
                                {
                                    reader.BaseStream.Seek(-4, SeekOrigin.Current);

                                    string outputPath = Path.ChangeExtension(Path.Combine(parameters.ExportDirectory, entry.Id.ToString()), ".ogg");
                                    using (var writer = File.OpenWrite(outputPath))
                                    {
                                        reader.BaseStream.CopyToLimited(writer, entry.FileLength - i);
                                    }
                                    break;
                                }
                                reader.BaseStream.Seek(-1, SeekOrigin.Current);
                            }
                            reader.BaseStream.Seek(-1, SeekOrigin.Current);
                        }
                        reader.BaseStream.Seek(-1, SeekOrigin.Current);
                    }
                }
            }
        }
    }
}
