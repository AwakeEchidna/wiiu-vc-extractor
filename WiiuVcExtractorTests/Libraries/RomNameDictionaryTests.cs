namespace WiiuVcExtractorTests.Libraries
{
    using System;
    using System.IO;
    using WiiuVcExtractor.Libraries;
    using Xunit;

    public class RomNameDictionaryTests
    {
        [Fact]
        public void RomNameDictionary_WhenCSVFileIsMissing_ThrowsException()
        {
            Assert.Throws<System.IO.FileNotFoundException>(() => new RomNameDictionary(string.Empty));
        }

        [Fact]
        public void RomNameDictionary_WhenCSVFileExists_ContructsRomNameDictionary()
        {
            var expected = typeof(RomNameDictionary);
            var result = new RomNameDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snesromnames.csv"));

            Assert.IsType(expected, result);
        }

        [Fact]
        public void GetRomName_WhenIDExists_ReturnsRomName()
        {
            var dictionary = new RomNameDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snesromnames.csv"));

            var result = dictionary.GetRomName("WUP-JDBE");

            Assert.Equal("Rival Turf", result);
        }

        [Fact]
        public void GetRomName_WhenIDDoesNotExist_ReturnsEmptyString()
        {
            var dictionary = new RomNameDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snesromnames.csv"));

            var result = dictionary.GetRomName("WUP-JUNKANDSUCH");

            Assert.Equal(string.Empty, result);
        }
    }
}
