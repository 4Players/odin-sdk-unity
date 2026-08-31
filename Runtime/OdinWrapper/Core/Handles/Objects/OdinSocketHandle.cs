using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// An opaque type representing an ODIN Socket handle, which is managed by the underlying ODIN room.
    /// This abstraction provides a high-level interface for room sockets, sending/receiving data,
    /// making it easier to integrate room-based interactions into your application.
    /// </summary>
    public class OdinSocketHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinSocketHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinSocketHandle(IntPtr handle) => new OdinSocketHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;
        private bool _useDeleter;

        /// <summary>
        /// Creates a new ODIN socket handle
        /// </summary>
        /// <remarks>A native socket is effectively in shared-connections thus there is no close/free/reset to perform. (Native free on room free by native)</remarks>
        /// <param name="handle">Socket handle pointer</param>
        /// <param name="useDeleter">Socket reset/close is not necessary</param>
        internal OdinSocketHandle(IntPtr handle, bool useDeleter = false)
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
                    Odin.Library.Methods.SocketReset(this);

                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
