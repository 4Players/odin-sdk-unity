using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Wrapper.Media;
using System;
using System.Linq;
using static OdinNative.Core.Imports.NativeBindings;

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
        public ulong Id { get; internal set; }
        internal ulong _Id { get; private set; }
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
        /// Flag if the native decoder handle is still usable, e.g. false after the peer left and the decoder was disposed
        /// </summary>
        public bool IsAlive => Handle?.IsAlive ?? false;

        /// <summary>
        /// Odin effect pipeline
        /// </summary>
        public MediaPipeline Pipeline { get; set; }

        public Utility.ChannelMask ChannelMask => GetChannels();
        public OdinPosition Position => GetPosition(ChannelMask);

        /// <summary>
        /// Channels this decoder accepts incoming datagrams for when routed by <see cref="Room.Room.Room_OnDatagram"/>.
        /// Defaults to <see cref="Utility.ChannelMask.All"/> so a single decoder receives every channel of its peer.
        /// </summary>
        /// <remarks>This is local routing state only and is not sent to the server; see <see cref="Room.Room.SetListenChannelMask"/> for that.</remarks>
        public Utility.ChannelMask ListenChannelMask { get; set; } = Utility.ChannelMask.All;

        /// <summary>
        /// Default value <c>null</c> indicates root or not set
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
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <returns>output media</returns>
        public static MediaDecoder Create(uint samplerate, bool stereo)
        {
            var result = Odin.Library.Methods.DecoderCreate(samplerate, stereo, out OdinDecoderHandle handle);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderCreate)} in {nameof(MediaDecoder.Create)} failed (invalid {handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})");

            if (Utility.IsOk(result) == false)
            {
                handle?.Dispose();
                return null;
            }

            MediaDecoder decoder = new MediaDecoder(handle)
            {
                _Id = (ulong)(IntPtr)handle,
                Id = (ulong)(IntPtr)handle, // callers may re-key by media id, see PeerEntity.GetOrCreateDecoder
                Samplerate = samplerate,
                Stereo = stereo
            };
            decoder.GetPipeline();

            return decoder;
        }

        /// <summary>
        /// Get native effect pipeline
        /// </summary>
        /// <returns>managed effect pipeline</returns>
        public MediaPipeline GetPipeline()
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(GetPipeline)} {nameof(MediaDecoder)} handle is released");

            if (Handle.IsAlive == false) return null;
            return Pipeline = new MediaPipeline(Odin.Library.Methods.DecoderGetPipeline(Handle));
        }

        /// <summary>
        /// Get native position in channel 
        /// </summary>
        /// <remarks>if mask contains more than one channel (Channel1 | Channel2) the result is one of the channels position (position: Channel1)</remarks>
        /// <param name="mask">channel mask to get position from</param>
        /// <returns>3d position array</returns>
        public OdinPosition[] GetPositions(Utility.ChannelMask mask)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(GetPositions)} {nameof(MediaDecoder)} handle is released");

            if (Handle.IsAlive == false) return null;
            var result = Odin.Library.Methods.DecoderGetPositions(Handle, mask, out OdinPosition[] positions);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderGetPositions)} in {nameof(MediaDecoder.GetPositions)}: {Utility.OdinLastErrorString()} (code {result})");
            return positions;
        }

        /// <summary>
        /// Get first native position of any channel 
        /// </summary>
        /// <param name="mask">channel mask to get position from</param>
        /// <returns>3d position</returns>
        public OdinPosition GetPosition(Utility.ChannelMask mask) => GetPositions(mask).FirstOrDefault();

        /// <summary>
        /// Get active native channels
        /// </summary>
        /// <returns>channel mask</returns>
        public Utility.ChannelMask GetChannels()
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(GetChannels)} {nameof(MediaDecoder)} handle is released");

            if (Handle.IsAlive == false) return Utility.ChannelMask.None;
            Utility.ChannelMask result = Odin.Library.Methods.DecoderGetActiveChannels(Handle);
            OdinLog.Assert(result != Utility.ChannelMask.None, $"{nameof(Odin.Library.Methods.DecoderGetActiveChannels)} in {nameof(MediaDecoder.GetChannels)}: {Utility.OdinLastErrorString()} value: {result}");
            return result;
        }

        /// <summary>
        /// This reflects the internal silence detection state of the decoder, which updates as audio is processed
        /// </summary>
        /// <remarks>Always returns true on invalid handle.</remarks>
        /// <returns>Returns whether the decoder is currently processing silence</returns>
        public bool GetIsSilent()
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(GetIsSilent)} {nameof(MediaDecoder)} handle is released");
            if (Handle.IsAlive == false) return true; // skip call since native result will always be: true

            return Odin.Library.Methods.DecoderIsSilent(Handle);
        }

        /// <summary>
        /// Pop output audio from the media
        /// </summary>
        /// <param name="audio">samples</param>
        /// <param name="isSilent">flag whether the popped buffer contains silence</param>
        /// <returns>true on success or false</returns>
        public bool Pop(ref float[] audio, out bool isSilent)
        {
            if (IsAlive == false) { isSilent = true; return false; }

            var result = Odin.Library.Methods.DecoderPop(Handle, ref audio, out isSilent);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderPop)} in {nameof(MediaDecoder.Pop)}: {Utility.OdinLastErrorString()} (code {result})");

            return Utility.IsOk(result);
        }

        /// <summary>
        /// Push output audio into the media for effect processing
        /// </summary>
        /// <param name="datagram">samples</param>
        /// <returns>true on success or false</returns>
        public bool Push(float[] datagram)
        {
            // the bindings take a raw pointer, so keep the native decoder alive here;
            // a concurrent Dispose (e.g. peer left on the rpc thread) defers the free
            bool refAdded = false;
            try { Handle.DangerousAddRef(ref refAdded); }
            catch (ObjectDisposedException) { return false; }
            try
            {
                var result = Odin.Library.Methods.DecoderPush(Handle, datagram);
                OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderPush)} in {nameof(MediaDecoder.Push)}: {Utility.OdinLastErrorString()} (code {result})");

                return Utility.IsOk(result);
            }
            finally
            {
                if (refAdded) Handle.DangerousRelease();
            }
        }

        protected internal bool Push(IntPtr datagramPtr, int datagramLength)
        {
            // see Push(float[]) for the DangerousAddRef rationale
            bool refAdded = false;
            try { Handle.DangerousAddRef(ref refAdded); }
            catch (ObjectDisposedException) { return false; }
            try
            {
                var result = Odin.Library.Methods.DecoderPush(Handle, datagramPtr, (uint)datagramLength);
                OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.DecoderPush)} in {nameof(MediaDecoder.Push)}: {Utility.OdinLastErrorString()} (code {result})");

                return Utility.IsOk(result);
            }
            finally
            {
                if (refAdded) Handle.DangerousRelease();
            }
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
