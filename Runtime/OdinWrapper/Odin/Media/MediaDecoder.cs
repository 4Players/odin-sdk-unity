using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Wrapper.Media;
using System;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Odin Playback Media
    /// </summary>
    public class MediaDecoder : IMedia, IDisposable
    {
        /// <summary>
        /// Media id
        /// </summary>
        public ushort Id { get; private set; }
        /// <summary>
        /// Arbitrary media data
        /// </summary>
        public MediaRpcProperties MediaProperties { get; internal set; }
        public bool IsPaused { get; internal set; }
        /// <summary>
        /// Output samplerate
        /// </summary>
        public uint Samplerate { get; private set; }
        /// <summary>
        /// Output channel flag
        /// </summary>
        public bool Stereo { get; private set; }
        internal OdinDecoderHandle Handle { get; private set; }

        /// <summary>
        /// Odin effect pipeline
        /// </summary>
        public MediaPipeline Pipeline { get; set; }

        /// <summary>
        /// Default value <code>null</code> indicates root or not set
        /// </summary>
        public IPeer Parent { get; set; }

        internal MediaDecoder(OdinDecoderHandle handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Create a new dangling output media.
        /// </summary>
        /// <remarks>Is not automatically assigned to a managed room yet but exists in a specific native room only.</remarks>
        /// <param name="mediaId">output media id</param>
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <returns>output media</returns>
        public static MediaDecoder Create(ushort mediaId, uint samplerate, bool stereo)
        {
            var result = Odin.Library.Methods.DecoderCreate(mediaId, samplerate, stereo, out OdinDecoderHandle handle);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderCreate)} in {nameof(MediaDecoder.Create)} failed (invalid {handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})");

            MediaDecoder decoder = new MediaDecoder(handle)
            {
                Id = mediaId,
                Samplerate = samplerate,
                Stereo = stereo
            };

            if (Utility.IsOk(result))
                decoder.GetPipeline();

            return decoder;
        }

        /// <summary>
        /// Get native effect pipeline
        /// </summary>
        /// <returns>managed effect pipeline</returns>
        public MediaPipeline GetPipeline()
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(GetPipeline)} {nameof(MediaDecoder)} handle is released");

            if (Handle.IsAlive == false) return null;
            return Pipeline = new MediaPipeline(Odin.Library.Methods.DecoderGetPipeline(Handle));
        }

        /// <summary>
        /// Pop output audio from the media
        /// </summary>
        /// <param name="audio">samples</param>
        /// <returns>true on success or false</returns>
        public bool Pop(ref float[] audio, out bool isSilent)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Pop)} {nameof(MediaDecoder)} handle is released");

            var result = Odin.Library.Methods.DecoderPop(Handle, ref audio, out isSilent);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderPop)} in {nameof(MediaDecoder.Pop)} failed: {Utility.OdinLastErrorString()} (code {result})");

            return Utility.IsOk(result);
        }

        /// <summary>
        /// Push output audio into the media for effect processing
        /// </summary>
        /// <param name="datagram">samples</param>
        /// <returns>true on success or false</returns>
        public bool Push(float[] datagram)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Push)} {nameof(MediaDecoder)} handle is released");

            var result = Odin.Library.Methods.DecoderPush(Handle, datagram);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderPush)} in {nameof(MediaDecoder.Push)} failed: {Utility.OdinLastErrorString()} (code {result})");

            return Utility.IsOk(result);
        }

        protected internal bool Push(IntPtr datagramPtr, int datagramLength)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Push)} {nameof(MediaDecoder)} handle is released");

            var result = Odin.Library.Methods.DecoderPush(Handle, datagramPtr, (uint)datagramLength);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderPush)} in {nameof(MediaDecoder.Push)} failed: {Utility.OdinLastErrorString()} (code {result})");

            return Utility.IsOk(result);
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
        /// Free native media decoder
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
