using System;
using System.Collections.Specialized;
using System.IO;

namespace WiiuVcExtractor.Libraries
{
    class RomNameDictionary
    {
        private OrderedDictionary dictionary;

        public RomNameDictionary(string dictionaryCsvPath)
        {
            if (!File.Exists(dictionaryCsvPath))
            {
                throw new FileNotFoundException("Could not find the " + dictionaryCsvPath + " file");
            }

            // The key of the dictionary is the WUP- string (WUP-FAME) and the value is the rom name
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
                        dictionary.Add(values[0], values[1]);
                    }
                }
            }
        }

        public string getRomName(string wupString)
        {
            if (dictionary.Contains(wupString))
            {
                return (string)dictionary[wupString];
            }

            return "";
        }
    }
}
