using System;
using System.IO;
using System.Runtime.InteropServices;
using OdinNative.Core;
using OdinNative.Core.Handles;
using OdinNative.Core.Imports;
using OdinNative.Core.Platform;
using Xunit;

namespace OdinNative.Tests
{
    /// <summary>
    /// Loads the native ODIN library once for all tests in the "Native" collection.
    /// The library directory is taken from the ODIN_NATIVE_LIB_DIR environment variable.
    /// Tests are skipped only when the variable is not set at all; once the library was
    /// requested, a missing directory or failed initialization fails the tests instead
    /// of silently skipping them.
    /// </summary>
    public sealed class NativeLibraryFixture
    {
        public string LibraryDirectory { get; }
        public string SkipReason { get; }

        public NativeLibraryFixture()
        {
            LibraryDirectory = Environment.GetEnvironmentVariable("ODIN_NATIVE_LIB_DIR");
            if (string.IsNullOrEmpty(LibraryDirectory))
            {
                SkipReason = "ODIN_NATIVE_LIB_DIR is not set";
                return;
            }

            if (Directory.Exists(LibraryDirectory) == false)
                throw new DirectoryNotFoundException($"ODIN_NATIVE_LIB_DIR \"{LibraryDirectory}\" does not exist");

            if (OdinCore<OdinLibraryHandle, NativeLibraryMethods>.IsInitialized == false)
            {
                // deliberately unguarded: an exception here fails every native test
                OdinCore<OdinLibraryHandle, NativeLibraryMethods>.Initialize(
                    new OdinLibraryParameters(LibraryDirectory, CurrentPlatform()));
            }

            if (OdinCore<OdinLibraryHandle, NativeLibraryMethods>.IsInitialized == false)
                throw new InvalidOperationException($"native library from \"{LibraryDirectory}\" failed to initialize");
        }

        public void SkipUnlessLoaded() => Skip.If(SkipReason != null, SkipReason);

        private static SupportedPlatform CurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return SupportedPlatform.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return SupportedPlatform.MacOSX;
            return SupportedPlatform.Linux;
        }
    }

    [CollectionDefinition("Native")]
    public class NativeCollection : ICollectionFixture<NativeLibraryFixture> { }
}
