using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// Optional, pluggable encryption module for room communications. A cipher can be attached to an
    /// ODIN room handle on creation to enable customizable, end-to-end encryption (E2EE). When enabled,
    /// it intercepts data right before transmission and immediately after reception, allowing custom
    /// processing of datagrams, messages and custom peer user data. The structure provides a suite of
    /// callback functions for initialization, cleanup, event handling and encryption/decryption tasks,
    /// along with parameters to adjust for any additional capacity overhead.
    /// </summary>
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
