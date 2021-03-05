namespace WiiuVcExtractor.Libraries
{
    using System.Collections.Specialized;
    using System.IO;

    /// <summary>
    /// Dictionary that maps WUP- strings to rom names.
    /// </summary>
    public class RomNameDictionary
    {
        private OrderedDictionary dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="RomNameDictionary"/> class from a CSV file.
        /// The CSV file should have no headers and two fields, the WUP ID and the rom name.
        /// </summary>
        /// <param name="dictionaryCsvPath">path to the CSV file to read.</param>
        public RomNameDictionary(string dictionaryCsvPath)
        {
            if (!File.Exists(dictionaryCsvPath))
            {
                throw new FileNotFoundException("Could not find the " + dictionaryCsvPath + " file");
            }

            // The key of the dictionary is the WUP- string (WUP-FAME) and the value is the rom name
            this.dictionary = new OrderedDictionary();

            // Read in the CSV file
            using var reader = new StreamReader(File.OpenRead(dictionaryCsvPath));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                if (!string.IsNullOrEmpty(values[0]) && !string.IsNullOrEmpty(values[1]))
                {
                    this.dictionary.Add(values[0], values[1]);
                }
            }
        }

        /// <summary>
        /// Gets a rom name using a WUP ID.
        /// </summary>
        /// <param name="wupString">WUP ID to find.</param>
        /// <returns>Aliased rom name or an empty string if the WUP ID cannot be found.</returns>
        public string GetRomName(string wupString)
        {
            if (this.dictionary.Contains(wupString))
            {
                return (string)this.dictionary[wupString];
            }

            return string.Empty;
        }
    }
}
