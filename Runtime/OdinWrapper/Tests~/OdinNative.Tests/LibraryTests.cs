using OdinNative.Core.Imports;
using Xunit;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Tests
{
    [Collection("Native")]
    public class LibraryTests
    {
        private readonly NativeLibraryFixture _fixture;

        public LibraryTests(NativeLibraryFixture fixture) => _fixture = fixture;

        private NativeLibraryMethods Methods => OdinNative.Odin.Library.Methods;

        [SkippableFact]
        public void Library_Initializes()
        {
            _fixture.SkipUnlessLoaded();
            Assert.True(OdinNative.Odin.Library.IsInitialized);
        }

        [SkippableFact]
        public void Initialize_RejectsUnsupportedVersion()
        {
            _fixture.SkipUnlessLoaded();
            var result = Methods.Initialize("9.9.9");
            Assert.NotEqual(OdinError.ODIN_ERROR_SUCCESS, result);
        }

        [SkippableFact]
        public void CreateAccessKey_ReturnsKey()
        {
            _fixture.SkipUnlessLoaded();
            string key = OdinNative.Wrapper.OdinClient.CreateAccessKey();
            Assert.False(string.IsNullOrEmpty(key));
            Assert.Equal(44, key.Length);
        }

        [SkippableFact]
        public void TokenGenerator_RoundtripsAccessKeyAndSignsToken()
        {
            _fixture.SkipUnlessLoaded();
            string key = OdinNative.Wrapper.OdinClient.CreateAccessKey();
            var result = Methods.TokenGeneratorCreate(key, out var generator);
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, result);
            using (generator)
            {
                Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.TokenGeneratorGetAccessKey(generator, out string keyBack));
                Assert.Equal(key, keyBack);
                Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.TokenGeneratorSign(generator, "{\"rid\":\"test-room\",\"uid\":\"test-user\"}", out string token));
                Assert.False(string.IsNullOrEmpty(token));
            }
        }
    }
}
