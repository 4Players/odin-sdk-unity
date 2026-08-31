using System.Runtime.InteropServices;
using Xunit;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Tests
{
    /// <summary>
    /// Pins the marshaled size of every struct that crosses the FFI boundary to the
    /// size of its native counterpart in odin.h. A mismatch here means memory corruption.
    /// </summary>
    public class StructLayoutTests
    {
        [Fact]
        public void OdinApmConfig_MatchesNativeSize() =>
            Assert.Equal(12, Marshal.SizeOf<OdinApmConfig>());

        [Fact]
        public void OdinSensitivityConfig_MatchesNativeSize() =>
            Assert.Equal(12, Marshal.SizeOf<OdinSensitivityConfig>());

        [Fact]
        public void OdinVadConfig_MatchesNativeSize() =>
            Assert.Equal(24, Marshal.SizeOf<OdinVadConfig>());

        [Fact]
        public void OdinViConfig_MatchesNativeSize() =>
            Assert.Equal(8, Marshal.SizeOf<OdinViConfig>());

        [Fact]
        public void OdinDatagramProperties_MatchesNativeSize() =>
            Assert.Equal(40, Marshal.SizeOf<OdinDatagramProperties>());
    }
}
