using System;
using System.Collections.Specialized;
using System.IO;

namespace WiiuVcExtractor.Libraries
{
    class RomSizeDictionary
    {
        private OrderedDictionary dictionary;

        public RomSizeDictionary(string dictionaryCsvPath)
        {
            if (!File.Exists(dictionaryCsvPath))
            {
                throw new FileNotFoundException("Could not find the " + dictionaryCsvPath + " file");
            }

            // The key of the dictionary is the rom name and the value is the size in bytes
            dictionary = new OrderedDictionary();

            // Read in the CSV file
            using (var reader = new StreamReader(File.OpenRead(dictionaryCsvPath)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (!String.IsNullOrEmpty(values[0]) && !String.IsNullOrEmpty(values[1]))
                    {
                        int romSize = Convert.ToInt32(values[1]);
                        dictionary.Add(values[0], romSize);
                    }
                }
            }
        }

        public int GetRomSize(string romName, int maxRomSize = 0)
        {
            if (dictionary.Contains(romName))
            {
                return (int)dictionary[romName];
            }

            romName = romName.Trim();

            if (dictionary.Contains(romName))
            {
                return (int)dictionary[romName];
            }

            return maxRomSize;
        }
    }
}
