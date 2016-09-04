namespace WiiuVcExtractor.RomExtractors
{
    interface IRomExtractor
    {
        bool IsValidRom();
        string ExtractRom(); 
    }
}
