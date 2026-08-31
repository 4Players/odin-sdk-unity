using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// Represents a decoder for media streams from remote voice chat clients, which encapsulates all
    /// the components required to process incoming audio streams. It includes an egress resampler for
    /// sample rate conversion, an Opus decoder for decompressing audio data and a customizable audio
    /// pipeline that enables the application of effects to modify the raw audio samples.
    /// </summary>
    public class OdinDecoderHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinDecoderHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static implicit operator OdinDecoderHandle(IntPtr handle) => new OdinDecoderHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;
        private bool _useDeleter;

        /// <summary>
        /// Creates a new ODIN decoder handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeLibraryMethods.OdinDecoderFreeDelegate"/></remarks>
        /// <param name="handle">Decoder handle pointer</param>
        /// <param name="useDeleter">On release handle calls the corresponding native destroy</param>
        internal OdinDecoderHandle(IntPtr handle, bool useDeleter = true)
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
                    Odin.Library.Methods.DecoderFree(this);

                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
