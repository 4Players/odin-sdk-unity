using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinDecoderHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinDecoderHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static implicit operator OdinDecoderHandle(IntPtr handle) => new OdinDecoderHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;

        /// <summary>
        /// Creates a new ODIN decoder handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinDecoderFreeDelegate"/></remarks>
        /// <param name="handle">Decoder handle pointer</param>
        internal OdinDecoderHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
            _isReleased = false;
        }

        protected override bool ReleaseHandle()
        {
            if (Odin.Library.IsInitialized)
            {
                Odin.Library.Methods.DecoderFree(this);
                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
