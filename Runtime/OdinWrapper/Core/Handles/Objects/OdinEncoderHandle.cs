using Microsoft.Win32.SafeHandles;
using System;

namespace OdinNative.Core.Imports
{
    /// <summary>
    /// Represents an encoder for local media streams, which encapsulates the components required to
    /// process outgoing audio streams captured from local sources (e.g. a microphone). It includes
    /// an ingress resampler for sample rate conversion, an Opus encoder for compressing the audio
    /// data and a customizable audio pipeline that allows the application of effects to modify the
    /// raw audio samples before transmission.
    /// </summary>
    public class OdinEncoderHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(OdinEncoderHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        public static explicit operator OdinEncoderHandle(IntPtr handle) => new OdinEncoderHandle(handle);

        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && _isReleased == false; } }
        private bool _isReleased;
        private bool _useDeleter;

        /// <summary>
        /// Creates a new ODIN encoder handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeLibraryMethods.OdinEncoderFreeDelegate"/></remarks>
        /// <param name="handle">Encoder handle pointer</param>
        /// <param name="useDeleter">On release handle calls the corresponding native destroy</param>
        internal OdinEncoderHandle(IntPtr handle, bool useDeleter = true)
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
                    Odin.Library.Methods.EncoderFree(this);

                _isReleased = true;
            }

            SetHandleAsInvalid();
            return true;
        }
    }
}
