using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinRoomHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinRoomHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinRoomHandle(IntPtr handle) => new OdinRoomHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;

        /// <summary>
        /// Creates a new ODIN room handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinRoomFreeDelegate"/></remarks>
        /// <param name="handle">Room handle pointer</param>
        internal OdinRoomHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
            _isReleased = false;
        }

        protected override bool ReleaseHandle()
        {
            if (Odin.Library.IsInitialized)
            {
                Odin.Library.Methods.RoomFree(this);
                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
