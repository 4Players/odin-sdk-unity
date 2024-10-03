using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    public class OdinPipelineHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinPipelineHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinPipelineHandle(IntPtr handle) => new OdinPipelineHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;

        /// <summary>
        /// Creates a new ODIN pipeline handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle will be set as invalid</remarks>
        /// <param name="handle">Pipeline handle pointer</param>
        internal OdinPipelineHandle(IntPtr handle)
            : base(ownsHandle: true)
        {
            SetHandle(handle);
            _isReleased = false;
        }

        protected override bool ReleaseHandle()
        {
            _isReleased = true;
            SetHandleAsInvalid();
            return true;
        }
    }
}
