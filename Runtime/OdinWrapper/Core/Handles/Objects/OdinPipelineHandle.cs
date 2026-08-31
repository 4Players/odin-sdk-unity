using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// A highly dynamic audio processing chain that manages a thread-safe collection of filters like
    /// voice activity detection, echo cancellation, noise suppression and even custom effects. This
    /// allows sequential processing and real-time modification of audio streams through operations
    /// like insertion, removal, reordering and configuration updates.
    /// </summary>
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
