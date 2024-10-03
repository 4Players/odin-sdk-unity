using Microsoft.Win32.SafeHandles;
using OdinNative.Wrapper;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinCipherHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinCipherHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinCipherHandle(IntPtr handle) => new OdinCipherHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false; } }

        /// <summary>
        /// Creates a new ODIN-Crypto cipher handle
        /// </summary>
        /// <param name="handle">Cipher handle pointer</param>
        internal OdinCipherHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            SetHandleAsInvalid();
            return true;
        }
    }
}
