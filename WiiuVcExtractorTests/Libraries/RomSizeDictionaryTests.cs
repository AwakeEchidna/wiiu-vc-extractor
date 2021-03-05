namespace WiiuVcExtractorTests.Libraries
{
    using System;
    using System.IO;
    using WiiuVcExtractor.Libraries;
    using Xunit;

    public class RomSizeDictionaryTests
    {
        [Fact]
        public void RomSizeDictionary_WhenCSVFileIsMissing_ThrowsException()
        {
            Assert.Throws<System.IO.FileNotFoundException>(() => new RomSizeDictionary(string.Empty));
        }

        [Fact]
        public void RomSizeDictionary_WhenCSVFileExists_ContructsRomNameDictionary()
        {
            var expected = typeof(RomSizeDictionary);
            var result = new RomSizeDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snesromsizes.csv"));

            Assert.IsType(expected, result);
        }

        [Fact]
        public void GetRomSize_WhenRomNameExists_ReturnsRomSize()
        {
            var dictionary = new RomSizeDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snesromsizes.csv"));

            var result = dictionary.GetRomSize("ROCKMAN SOCCER");

            Assert.Equal(1310720, result);
        }

        [Fact]
        public void GetRomName_WhenRomNameDoesNotExist_ReturnsMaxRomSize()
        {
            var dictionary = new RomSizeDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snesromsizes.csv"));

            var result = dictionary.GetRomSize("WUP-JUNKANDSUCH", 1024000);

            Assert.Equal(1024000, result);
        }
    }
}
