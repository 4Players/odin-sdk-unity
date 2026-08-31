using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// A handle for generating ODIN tokens, employed for generating signed room tokens predicated on
    /// an access key. Be aware that access keys serve as your unique authentication keys, requisite for
    /// generating room tokens to access the ODIN server network. To ensure your security, it's strongly
    /// recommended that you _NEVER_ embed an access key within your client code and instead generate
    /// room tokens on a server.
    /// </summary>
    public class OdinTokenGeneratorHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinTokenGeneratorHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinTokenGeneratorHandle(IntPtr handle) => new OdinTokenGeneratorHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;
        private bool _useDeleter;

        /// <summary>
        /// Creates a new ODIN token generator handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeLibraryMethods.OdinTokenGeneratorFreeDelegate"/></remarks>
        /// <param name="handle">TokenGenerator handle pointer</param>
        /// <param name="useDeleter">On release handle calls the corresponding native destroy</param>
        internal OdinTokenGeneratorHandle(IntPtr handle, bool useDeleter = true)
            : base(true)
        {
            SetHandle(handle);
            _isReleased = false;
            _useDeleter = useDeleter;
        }

        protected override bool ReleaseHandle()
        {
            if (Odin.Library.IsInitialized)
            {
                if(_useDeleter)
                    Odin.Library.Methods.TokenGeneratorFree(this);

                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
