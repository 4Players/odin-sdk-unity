using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// An opaque type representing an ODIN room handle, which is managed by the underlying connection.
    /// This abstraction provides a high-level interface for joining rooms, managing persistent state
    /// and sending/receiving data, making it easier to integrate room-based interactions into your
    /// application.
    /// </summary>
    public class OdinRoomHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinRoomHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinRoomHandle(IntPtr handle) => new OdinRoomHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;
        private bool _useDeleter;

        /// <summary>
        /// Creates a new ODIN room handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeLibraryMethods.OdinRoomFreeDelegate"/></remarks>
        /// <param name="handle">Room handle pointer</param>
        /// <param name="useDeleter">On release handle calls the corresponding native destroy</param>
        internal OdinRoomHandle(IntPtr handle, bool useDeleter = true)
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
                    Odin.Library.Methods.RoomFree(this);

                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
