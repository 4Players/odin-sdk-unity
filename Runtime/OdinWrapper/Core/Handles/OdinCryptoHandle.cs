using OdinNative.Core.Platform;
using System;
using System.Linq;

namespace OdinNative.Core.Handles
{
    public class OdinCryptoHandle : OdinHandle
    {
        private const string WindowsCryptoName = "odin_crypto.dll";
        private const string LinuxCryptoName = "libodin_crypto.so";
        private const string AppleCryptoName = "libodin_crypto.dylib";

        /// <summary>
        /// Combine the library name to the path and add an empty relative path with only a library
        /// </summary>
        /// <remarks>Basic <code>"PATH/FILE"</code> concatenation. Path combine is not supported on all platforms.</remarks>
        /// <param name="platform">The platform to select the filename</param>
        /// <param name="possiblePaths">The paths to look for</param>
        /// <returns>Fullname array</returns>
        private static string[] _SetNamedLib(SupportedPlatform platform, string[] possiblePaths)
        {
            Func<string[], string, string[]> merge = (paths, lib) => paths
                .Select(path => string.Format("{0}/{1}", path, lib))
                .Concat(new[] { lib })
                .ToArray();

            if (platform.HasFlag(SupportedPlatform.Windows))
                return merge(possiblePaths, WindowsCryptoName);
            else if (platform.HasFlag(SupportedPlatform.Linux) || platform.HasFlag(SupportedPlatform.Android))
                return merge(possiblePaths, LinuxCryptoName);
            else if (platform.HasFlag(SupportedPlatform.MacOSX) || platform.HasFlag(SupportedPlatform.iOS))
                return merge(possiblePaths, AppleCryptoName);

            return possiblePaths;
        }

        public OdinCryptoHandle(SupportedPlatform platform, string[] possiblePaths)
            : base(platform, _SetNamedLib(platform, possiblePaths))
        {
            // no init call for crypto
            IsInitialized = true;
        }
    }
}
