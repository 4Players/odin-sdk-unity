using OdinNative.Core.Handles;
using OdinNative.Core.Imports;
using OdinNative.Core.Platform;
using System;
using System.Threading;

namespace OdinNative.Core
{
    /// <summary>
    /// Native library initializer
    /// </summary>
    public static class OdinCore<T, U> 
        where T : OdinHandle
        where U : NativeMethods<T>
    {
        private static T Handle;
        private static NativeMethods<T> NativeMethods;

        private static ReaderWriterLock InitializedLock = new ReaderWriterLock();
        private static bool ProcessExitRegistered = false;

        internal static NativeMethods<T> Api
        {
            get
            {
                InitializedLock.AcquireReaderLock(1000);
                try
                {
                    if (IsInitialized == false)
                    {
                        LockCookie cookie = InitializedLock.UpgradeToWriterLock(Timeout.Infinite);
                        try
                        {
                            if (IsInitialized == false)
                                Initialize();
                        }
                        finally
                        {
                            InitializedLock.DowngradeFromWriterLock(ref cookie);
                        }
                    }
                    return NativeMethods;
                }
                finally
                {
                    InitializedLock.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// Indicates whether or not the native ODIN runtime has been loaded and initialized
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                return Handle != null && Handle.IsClosed == false && Handle.IsInvalid == false && Handle.IsInitialized;
            }
        }

        /// <summary>
        /// Initializes the native ODIN runtime
        /// </summary>
        /// <remarks>
        /// This function explicitly loads the ODIN library. It will be invoked automatically by the SDK when required. <b>Not supported in WebGL</b>
        /// </remarks>
        public static void Initialize()
        {
#if UNITY_WEBGL
            throw new NotSupportedException("Native loading without LLVM bitcode is not supported!");
#else
            Initialize(new OdinLibraryParameters());
#endif
        }

        /// <summary>
        /// Creates a new library-Instance
        /// </summary>
        /// <param name="parameters">Information used to create the instance</param>
        /// <exception cref="System.InvalidOperationException">a library is already created</exception>
        /// <exception cref="System.NullReferenceException"><paramref name="parameters"/> is null</exception>
        public static void Initialize(OdinLibraryParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            InitializedLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (Handle != null) throw new InvalidOperationException("Library is already initialized");
                if (ProcessExitRegistered == false)
                {
                    AppDomain.CurrentDomain.ProcessExit += ProcessExit;
                    ProcessExitRegistered = true;
                }
                Platform = parameters.Platform;
                Handle = (T)Activator.CreateInstance(typeof(T), Platform, parameters.PossibleNativeBinaryLocations);
                NativeBinary = Handle.Location;
                NativeMethods = (U)Activator.CreateInstance(typeof(U), Handle);
            }
            catch
            {
                Handle?.Dispose();
                Handle = null;
                throw;
            }
            finally
            {
                InitializedLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the wrapper
        /// </summary>
        public static void Release()
        {
            InitializedLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Handle?.Dispose();
                Handle = null;
            }
            finally
            {
                InitializedLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Location of the native ODIN runtime binary
        /// </summary>
        public static string NativeBinary { get; private set; }

        /// <summary>
        /// Platform the library is running on
        /// </summary>
        /// <remarks>
        /// This value is used to determine how the native ODIN runtime library will loaded and unloaded.
        /// </remarks>
        public static SupportedPlatform Platform { get; private set; }

        private static void ProcessExit(object sender, EventArgs e)
        {
            if (IsInitialized)
                Release();
        }
    }
}
