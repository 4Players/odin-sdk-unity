using OdinNative.Core.Handles;
using OdinNative.Core.Imports;
using OdinNative.Core.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OdinNative.Core
{
    public static class OdinLibrary
    {
        private static OdinHandle Handle;
        private static NativeMethods NativeMethods;
        private static ReaderWriterLock InitializedLock = new ReaderWriterLock();
        private static bool ProcessExitRegistered = false;

        internal static NativeMethods Api
        {
            get
            {
                InitializedLock.AcquireReaderLock(Timeout.Infinite);
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
        /// true if the Odin library has been loaded and initialized; otherwise, false
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                OdinHandle handle = Handle;
                return handle != null && handle.IsClosed == false;
            }
        }

        /// <summary>
        /// Initializes the Odin clientlib
        /// </summary>
        /// <remarks>
        /// Explicitly loads the Odin clientlib. Will be automatically invoked by the SDK when required.
        /// </remarks>
        public static void Initialize()
        {
            Initialize(new OdinLibraryParameters());
        }

        /// <summary>
        /// Creates a new <see cref="OdinLibrary"/>-Instance
        /// </summary>
        /// <param name="parameters">Information used to create the instance</param>
        /// <exception cref="InvalidOperationException">a <see cref="OdinLibrary"/> is already created</exception>
        /// <exception cref="NullReferenceException"><paramref name="OdinLibraryParameters"/> is null</exception>
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
                Handle = OdinHandle.Load(Platform, parameters.PossibleNativeBinaryLocations);
                NativeBinary = Handle.Location;
                NativeMethods = new NativeMethods(Handle);
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
        /// Releases the unmanaged resources used by the <see cref="OdinLibrary"/>
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
        /// Location to the Odin library binary.
        /// </summary>
        public static string NativeBinary { get; private set; }

        /// <summary>
        /// Platform the library is running on
        /// </summary>
        /// <remarks>
        /// Used to determine how the native library will loaded and unloaded.
        /// </remarks>
        public static SupportedPlatform Platform { get; private set; }

        private static void ProcessExit(object sender, EventArgs e)
        {
            Release();
        }

        internal static Exception CreateException(int error, string extraMessage = null)
        {
            string message = Api.GetErrorMessage(error);
            OdinException result = new OdinException(error, message);
            if (extraMessage != null)
                result.Data.Add("extraMessage", extraMessage);
            return result;
        }
    }
}
