using OdinNative.Core.Imports;
using OdinNative.Core.Platform;
using OdinNative.Wrapper;
using System;
using System.Linq;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Handles
{
    public class OdinLibraryHandle : OdinHandle
    {
        private const string WindowsLibName = "odin.dll";
        private const string LinuxLibName = "libodin.so";
        private const string AppleLibName = "libodin.dylib";
        private const string IOSLibName = "Odin.framework/Odin";

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
                return merge(possiblePaths, WindowsLibName);
            else if (platform.HasFlag(SupportedPlatform.Linux) || platform.HasFlag(SupportedPlatform.Android))
                return merge(possiblePaths, LinuxLibName);
            else if (platform.HasFlag(SupportedPlatform.MacOSX))
                return merge(possiblePaths, AppleLibName);
            else if (platform.HasFlag(SupportedPlatform.iOS))
                return merge(possiblePaths, IOSLibName);

            return possiblePaths;
        }

        public OdinLibraryHandle(SupportedPlatform platform, string[] possiblePaths)
            : base(platform, _SetNamedLib(platform, possiblePaths))
        {
            NativeLibraryMethods.OdinInitializeDelegate startupClientLib;
            GetLibraryMethod("odin_initialize", out startupClientLib);
            var ret = startupClientLib(OdinNative.Core.Imports.NativeBindings.OdinLibraryVersion);
            if (ret == OdinError.ODIN_ERROR_SUCCESS)
                IsInitialized = true;
            else
            {
                NativeLibraryMethods.OdinErrorGetLastErrorDelegate getLastError;
                GetLibraryMethod("odin_error_get_last_error", out getLastError);
                string lastError = Native.TryReadCString(getLastError());
#pragma warning disable CS0618 //UNITY Type or member is obsolete
                Utility.Throw(new OdinWrapperException($"Native library will be in an invalid state. (odin_initialize {ret})", new NotSupportedException(lastError)));
#pragma warning restore CS0618 //UNITY Type or member is obsolete
            }
        }

        protected override bool ReleaseHandle()
        {
            bool result = true;
            try
            {
                NativeLibraryMethods.OdinShutdownDelegate shutdownClientLib;
                GetLibraryMethod("odin_shutdown", out shutdownClientLib);
                shutdownClientLib();
            }
            catch
            {
                result = false;
            }
            // release library
            result = base.ReleaseHandle();
            return result;
        }
    }
}
