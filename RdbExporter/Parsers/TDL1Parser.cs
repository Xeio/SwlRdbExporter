using RdbExporter.Entities;
using RdbExporter.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RdbExporter.Parsers
{
    public class TDL1Parser
    {
        public static TDL1File ParseTDL1File(string file)
        {
            var binaryReader = new BinaryReader(File.OpenRead(file));

            var header = Encoding.ASCII.GetString(binaryReader.ReadBytes(4));
            if (header != "TDL1") throw new InvalidOperationException("Not a TDL1 file.");

            var languageCode = binaryReader.ReadInt32PrefacedString();
            var languageNameInEnglish = binaryReader.ReadInt32PrefacedString();
            var languageNameInLanguage = binaryReader.ReadInt32PrefacedString();

            binaryReader.ReadInt32PrefacedString(); //String "(N == 1) /? 1 /: -1" is this some sort of expression? Probably no significance

            var entryCount = binaryReader.ReadInt32();

            var entries = ReadePairs(binaryReader, entryCount).ToList();

            binaryReader.ReadBytes(2); //What are these two bytes? Just a buffer? Both null so maybe

            var nameEntryCount = binaryReader.ReadInt32();
            var namesAndDescriptions = ReadeIdAndStringPairs(binaryReader, nameEntryCount).ToList();

            //Not sure what these last two groups of IDs mean. Some of them are duplicated numbers from the above entries and descriptions, others not.
            var unknownIdsCount = binaryReader.ReadInt32();
            var unknownIds = Enumerable.Range(0, unknownIdsCount).Select((i) => binaryReader.ReadInt32()).ToList();

            var unknownIdsCount2 = binaryReader.ReadInt32();
            var unknownIds2 = Enumerable.Range(0, unknownIdsCount2).Select((i) => binaryReader.ReadInt32()).ToList();

            var languageFileEntries = entries.Join(namesAndDescriptions, (entry) => entry.Item1, (nameAndDesc) => nameAndDesc.Item1,
                (entry, nameAndDesc) => new TDL1Entry()
                {
                    RdbId = entry.Item1,
                    FileId = entry.Item2,
                    FriendlyName = nameAndDesc.Item2,
                    Type = nameAndDesc.Item3
                }).ToList();

            return new TDL1File()
            {
                LanguageCode = languageCode,
                LanguageName = languageNameInEnglish,
                LanguageNameInLanguage = languageNameInLanguage,
                Entries = languageFileEntries
            };
        }

        static IEnumerable<Tuple<int, int>> ReadePairs(BinaryReader reader, int count)
        {
            for(int i = 0; i < count; i++)
            {
                yield return Tuple.Create(reader.ReadInt32(), reader.ReadInt32());
            }
        }
        static IEnumerable<Tuple<int, string, string>> ReadeIdAndStringPairs(BinaryReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Tuple.Create(reader.ReadInt32(), reader.ReadInt32PrefacedString(false), reader.ReadInt32PrefacedString(false));
            }
        }
    }
}
