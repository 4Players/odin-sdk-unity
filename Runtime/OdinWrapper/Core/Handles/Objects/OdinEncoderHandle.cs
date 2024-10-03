using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinEncoderHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinEncoderHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinEncoderHandle(IntPtr handle) => new OdinEncoderHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;

        /// <summary>
        /// Creates a new ODIN encoder handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinEncoderFreeDelegate"/></remarks>
        /// <param name="handle">Encoder handle pointer</param>
        internal OdinEncoderHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
            _isReleased = false;
        }

        protected override bool ReleaseHandle()
        {
            if (Odin.Library.IsInitialized)
            {
                Odin.Library.Methods.EncoderFree(this);
                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
