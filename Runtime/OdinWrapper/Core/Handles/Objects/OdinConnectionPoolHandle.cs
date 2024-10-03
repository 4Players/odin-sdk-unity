using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinConnectionPoolHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinConnectionPoolHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinConnectionPoolHandle(IntPtr handle) => new OdinConnectionPoolHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;

        /// <summary>
        /// Creates a new ODIN connection pool handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinConnectionPoolFreeDelegate"/></remarks>
        /// <param name="handle">ConnectionPool handle pointer</param>
        internal OdinConnectionPoolHandle(IntPtr handle)
            : base(ownsHandle: true)
        {
            SetHandle(handle);
            _isReleased = false;
        }

        protected override bool ReleaseHandle()
        {
            if (Odin.Library.IsInitialized)
            {
                Odin.Library.Methods.ConnectionPoolFree(this);
                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
