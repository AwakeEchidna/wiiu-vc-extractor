namespace WiiuVcExtractor.RomExtractors
{
    /// <summary>
    /// Rom extractor interface.
    /// </summary>
    internal interface IRomExtractor
    {
        /// <summary>
        /// Whether the rom is valid.
        /// </summary>
        /// <returns>true if valid, false otherwise.</returns>
        bool IsValidRom();

        /// <summary>
        /// Extracts a rom from a given file.
        /// </summary>
        /// <returns>path to the extracted rom.</returns>
        string ExtractRom();
    }
}
