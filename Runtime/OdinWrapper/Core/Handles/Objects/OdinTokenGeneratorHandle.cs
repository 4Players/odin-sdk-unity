using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinTokenGeneratorHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinTokenGeneratorHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinTokenGeneratorHandle(IntPtr handle) => new OdinTokenGeneratorHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;

        /// <summary>
        /// Creates a new ODIN token generator handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinTokenGeneratorFreeDelegate"/></remarks>
        /// <param name="handle">TokenGenerator handle pointer</param>
        internal OdinTokenGeneratorHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
            _isReleased = false;
        }

        protected override bool ReleaseHandle()
        {
            if (Odin.Library.IsInitialized)
            {
                Odin.Library.Methods.TokenGeneratorFree(this);
                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
