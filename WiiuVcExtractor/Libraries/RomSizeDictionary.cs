namespace WiiuVcExtractor.Libraries
{
    using System;
    using System.Collections.Specialized;
    using System.IO;

    /// <summary>
    /// Dictionary that rom names to rom sizes.
    /// </summary>
    public class RomSizeDictionary
    {
        private OrderedDictionary dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="RomSizeDictionary"/> class from a CSV file.
        /// The CSV file should have no headers and two fields, the rom name and the size in bytes.
        /// </summary>
        /// <param name="dictionaryCsvPath">path to the CSV file to read.</param>
        public RomSizeDictionary(string dictionaryCsvPath)
        {
            if (!File.Exists(dictionaryCsvPath))
            {
                throw new FileNotFoundException("Could not find the " + dictionaryCsvPath + " file");
            }

            // The key of the dictionary is the rom name and the value is the size in bytes
            this.dictionary = new OrderedDictionary();

            // Read in the CSV file
            using var reader = new StreamReader(File.OpenRead(dictionaryCsvPath));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                if (!string.IsNullOrEmpty(values[0]) && !string.IsNullOrEmpty(values[1]))
                {
                    int romSize = Convert.ToInt32(values[1]);
                    this.dictionary.Add(values[0], romSize);
                }
            }
        }

        /// <summary>
        /// Gets a rom size using a rom name.
        /// </summary>
        /// <param name="romName">rom name to find.</param>
        /// <param name="maxRomSize">maximum size of a rom in bytes.</param>
        /// <returns>Found rom size or the value of maxRomSize if an entry cannot be found.</returns>
        public int GetRomSize(string romName, int maxRomSize = 0)
        {
            if (this.dictionary.Contains(romName))
            {
                return (int)this.dictionary[romName];
            }

            romName = romName.Trim();

            if (this.dictionary.Contains(romName))
            {
                return (int)this.dictionary[romName];
            }

            return maxRomSize;
        }
    }
}
