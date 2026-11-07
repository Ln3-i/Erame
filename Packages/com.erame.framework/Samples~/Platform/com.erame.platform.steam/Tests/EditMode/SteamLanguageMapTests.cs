using NUnit.Framework;

namespace Erame.Platform.Steam.Tests{
    public class SteamLanguageMapTests{
        [TestCase("english", "English")]
        [TestCase("schinese", "Chinese")]
        [TestCase("tchinese", "Chinese")]
        [TestCase("ENGLISH", "English")]
        [TestCase("Schinese", "Chinese")]
        public void ToFrameworkLanguage_KnownCodes_MapToFrameworkLanguage(string steamLanguage, string expected){
            Assert.AreEqual(expected, SteamLanguageMap.ToFrameworkLanguage(steamLanguage));
        }

        [TestCase("japanese")]
        [TestCase("koreana")]
        [TestCase("")]
        [TestCase(null)]
        public void ToFrameworkLanguage_UnknownOrEmpty_ReturnsNull(string steamLanguage){
            Assert.IsNull(SteamLanguageMap.ToFrameworkLanguage(steamLanguage));
        }

        [Test]
        public void ToSteamLanguage_KnownLanguages_MapToSteamCode(){
            Assert.AreEqual("english", SteamLanguageMap.ToSteamLanguage("English"));
            Assert.AreEqual("schinese", SteamLanguageMap.ToSteamLanguage("Chinese"));
        }

        [Test]
        public void ToSteamLanguage_Unknown_ReturnsNull(){
            Assert.IsNull(SteamLanguageMap.ToSteamLanguage("Japanese"));
            Assert.IsNull(SteamLanguageMap.ToSteamLanguage(null));
        }

        [Test]
        public void SupportedSteamLanguages_ContainsMappedCodes(){
            CollectionAssert.Contains(SteamLanguageMap.SupportedSteamLanguages, "english");
            CollectionAssert.Contains(SteamLanguageMap.SupportedSteamLanguages, "schinese");
        }
    }
}
