using OdinNative.Core;
using OdinNative.Core.Handles;
using OdinNative.Core.Imports;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.OdinLog;

namespace OdinNative
{
    /// <summary>
    /// References to OdinCore native imports.
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="OdinNative.Odin.Library"/></term>
    /// <description>Static reference to the native imports of the odin library as <see cref="OdinNative.Core.Imports.NativeLibraryMethods"/></description>
    /// </item>
    /// <item>
    /// <term><see cref="OdinNative.Odin.Crypto"/></term>
    /// <description>Static reference to the native imports of the odin crypto extension library as <see cref="OdinNative.Core.Imports.NativeCryptoMethods"/></description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Using calls to an unloaded library will trigger the file load/initialize</remarks>
    public static class Odin
    {
        /// <summary>
        /// Log (default with Debug.WriteLine)
        /// </summary>
        /// <remarks><see cref="OdinNative.OdinLog.SetLogger"/> callback</remarks>
        public static void Log(string message, OdinLog.LogType type = LogType.Info, params object[] args) => OdinLog.Log(message, type, args);

        /// <summary>
        /// Native Odin functions
        /// </summary>
        /// <remarks>Same as <see cref="OdinNative.Odin.Library.Methods"/></remarks>
        public static NativeLibraryMethods Api => Library.Methods;

        /// <summary>
        /// Main OdinLibrary (e.g. odin.dll, libodin.so, ...)
        /// </summary>
        public static class Library
        {
            /// <summary>
            /// Static reference to the native function imports of the odin library
            /// </summary>
            /// <remarks>Will load and Initialize the library if needed</remarks>
            public static NativeLibraryMethods Methods => OdinCore<OdinLibraryHandle, NativeLibraryMethods>.Api as NativeLibraryMethods;
            /// <summary>
            /// Flag for valid library handle
            /// </summary>
            public static bool IsInitialized => OdinCore<OdinLibraryHandle, NativeLibraryMethods>.IsInitialized;
            /// <summary>
            /// Load and initialize the library
            /// </summary>
            public static void Initialize() => OdinCore<OdinLibraryHandle, NativeLibraryMethods>.Initialize();
            /// <summary>
            /// Unload and dispose the library
            /// </summary>
            public static void Release() => OdinCore<OdinLibraryHandle, NativeLibraryMethods>.Release();
        }

        /// <summary>
        /// Extension OdinCrypto (e.g. odin_crypto.dll, libodin_crypto.so, ...)
        /// </summary>
        public static class Crypto
        {
            /// <summary>
            /// Static reference to the native function imports of the odin crypto library
            /// </summary>
            /// <remarks>Will load and Initialize the library if needed</remarks>
            public static NativeCryptoMethods Methods => OdinCore<OdinCryptoHandle, NativeCryptoMethods>.Api as NativeCryptoMethods;
            /// <summary>
            /// Flag for valid library handle
            /// </summary>
            public static bool IsInitialized => OdinCore<OdinCryptoHandle, NativeCryptoMethods>.IsInitialized;
            /// <summary>
            /// Load and initialize the library
            /// </summary>
            public static void Initialize() => OdinCore<OdinCryptoHandle, NativeCryptoMethods>.Initialize();
            /// <summary>
            /// Unload and dispose the library
            /// </summary>
            public static void Release() => OdinCore<OdinCryptoHandle, NativeCryptoMethods>.Release();
        }

        internal static OdinException GetException(OdinError error, string extraMessage = null)
        {
            string message = Library.Methods.ErrorGetLastError();
            OdinException result = new OdinException(error, message);
            if (string.IsNullOrEmpty(extraMessage) == false)
                result.Data.Add("extraMessage", extraMessage);
            return result;
        }

    }
}
