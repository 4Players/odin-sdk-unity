using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Wrapper.Media;
using System;
using System.Collections.Concurrent;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Odin Capture Media
    /// </summary>
    public class MediaEncoder : IMedia, IDisposable
    {
        /// <summary>
        /// Media id
        /// </summary>
        public ushort Id { get; private set; }
        /// <summary>
        /// Arbitrary media data
        /// </summary>
        public MediaRpcProperties MediaProperties { get; internal set; }
        /// <summary>
        /// Input samplerate
        /// </summary>
        public uint Samplerate { get; private set; }
        /// <summary>
        /// Input channel flag
        /// </summary>
        public bool Stereo { get; private set; }
        internal OdinEncoderHandle Handle { get; private set;}
        /// <summary>
        /// Odin effect pipeline
        /// </summary>
        public MediaPipeline Pipeline { get; set; }
        /// <summary>
        /// On true <see cref="Push"/> <code>isSilent = true</code> will not push data to the native encoder
        /// </summary>
        /// <remarks>On true always returns <see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/></remarks>
        public bool GuardSilence { get; set; } = true;

        /// <summary>
        /// Default value <code>null</code> indicates root or not set
        /// </summary>
        public IPeer Parent { get; set; }

        internal MediaEncoder(OdinEncoderHandle handle) 
        {
            Handle = handle;
            MediaProperties = null;
        }

        /// <summary>
        /// Create a new dangling input media.
        /// </summary>
        /// <remarks>Is not automatically assigned to a managed room yet but exists in a specific native room only.</remarks>
        /// <param name="mediaId">input media id</param>
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <returns>input media</returns>
        public static MediaEncoder Create(ushort mediaId, uint samplerate, bool stereo)
        {
            var result = Odin.Library.Methods.EncoderCreate(samplerate, stereo, out OdinEncoderHandle handle);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderCreate)} in {nameof(MediaEncoder.Create)} failed (invalid {handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})");

            MediaEncoder encoder = new MediaEncoder(handle)
            {
                Id = mediaId,
                Samplerate = samplerate,
                Stereo = stereo
            };

            if (Utility.IsOk(result))
                encoder.GetPipeline();

            return encoder;
        }

        /// <summary>
        /// Get native effect pipeline
        /// </summary>
        /// <returns>managed effect pipeline</returns>
        public MediaPipeline GetPipeline()
        {
            if (OdinDefaults.Debug)
                Utility.Assert(Handle.IsAlive, $"{nameof(GetPipeline)} {nameof(MediaEncoder)} handle is released");

            if (Handle.IsAlive == false) return null;
            return Pipeline = new MediaPipeline(Odin.Library.Methods.EncoderGetPipeline(Handle));
        }

        /// <summary>
        /// Pop all audio datagrams into the stack
        /// </summary>
        /// <param name="datagrams">datagram stack</param>
        /// <returns>true on success or false</returns>
        public bool PopAll(ref ConcurrentStack<byte[]> datagrams)
        {
            OdinError error;
            do
            {
                error = this.Pop(out byte[] datagram);
                datagrams.Push(datagram);
            } while (error == OdinError.ODIN_ERROR_SUCCESS);

            Utility.Assert(error == OdinError.ODIN_ERROR_NO_DATA, $"{nameof(Odin.Library.Methods.EncoderPop)} in {nameof(MediaEncoder.PopAll)} unexpected: {Utility.OdinLastErrorString()} (code {error})");
            return error == OdinError.ODIN_ERROR_NO_DATA;
        }

        /// <summary>
        /// Pop one datagram from the media
        /// </summary>
        /// <param name="datagram"></param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/>/<see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_NO_DATA"/> or error</returns>
        public OdinError Pop(out byte[] datagram)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Pop)} {nameof(MediaEncoder)} handle is released");

            datagram = new byte[1920];
            var result = Odin.Library.Methods.EncoderPop(Handle, new ushort[] { Id }, ref datagram);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderPop)} in {nameof(MediaEncoder.Pop)} failed: {Utility.OdinLastErrorString()} (code {result})");
            return result;
        }

        /// <summary>
        /// Push samples to the media
        /// </summary>
        /// <param name="samples">audio samples</param>
        /// <param name="isSilent">flag samples as silence</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        public OdinError Push(float[] samples)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Push)} {nameof(MediaEncoder)} handle is released");

            var result = Odin.Library.Methods.EncoderPush(Handle, samples);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderPush)} in {nameof(MediaEncoder.Push)} failed: {Utility.OdinLastErrorString()} (code {result})");
            return result;
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Pipeline = null;
                    Handle?.Dispose();
                }
                disposedValue = true;
            }
        }

        /// <summary>
        /// Free native media encoder
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
