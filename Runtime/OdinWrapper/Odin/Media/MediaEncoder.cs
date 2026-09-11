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
        public ulong Id { get; private set; }
        internal ulong _Id => Id;

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
        /// Flag if the native encoder handle is still usable, e.g. false after the owning room was disposed
        /// </summary>
        public bool IsAlive => Handle?.IsAlive ?? false;
        /// <summary>
        /// Odin effect pipeline
        /// </summary>
        public MediaPipeline Pipeline { get; set; }

        /// <summary>
        /// Channels the encoder currently has a position for and therefore transmits on
        /// </summary>
        /// <remarks>Tracks <see cref="SetPosition"/>, <see cref="ClearPosition"/> and <see cref="SetChannels"/></remarks>
        public Utility.ChannelMask ChannelMask { get; private set; }
        /// <summary>
        /// Last position set with <see cref="SetPosition"/>
        /// </summary>
        public OdinPosition Position { get; private set; }
        public byte[] AddtionalData { get; private set; }

        /// <summary>
        /// Silence guard flag
        /// </summary>
        public bool GuardSilence { get; set; } = true;

        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        public IPeer Parent { get; set; }

        internal MediaEncoder(OdinEncoderHandle handle) 
        {
            Handle = handle;
        }

        /// <summary>
        /// Create a new dangling input media.
        /// </summary>
        /// <remarks>Is not automatically assigned to a managed room yet but exists in a specific native room only.</remarks>
        /// <param name="peerId">peer id</param>
        /// <param name="samplerate">samplerate</param>
        /// <param name="stereo">stereo flag</param>
        /// <returns>input media</returns>
        public static MediaEncoder Create(uint peerId, uint samplerate, bool stereo)
        {
            var result = Odin.Library.Methods.EncoderCreate(peerId, samplerate, stereo, out OdinEncoderHandle handle);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderCreate)} in {nameof(MediaEncoder.Create)} failed (invalid {handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})");

            if (Utility.IsOk(result) == false)
            {
                handle?.Dispose();
                return null;
            }

            MediaEncoder encoder = new MediaEncoder(handle)
            {
                Id = (ulong)(IntPtr)handle,
                Samplerate = samplerate,
                Stereo = stereo
            };

            var defaultChannels = Utility.ChannelMask.Channel1 | Utility.ChannelMask.Channel2;
            encoder.SetChannels(defaultChannels);
            encoder.SetPosition(defaultChannels, new OdinPosition());
            encoder.GetPipeline();

            return encoder;
        }

        /// <summary>
        /// Get native effect pipeline
        /// </summary>
        /// <returns>managed effect pipeline</returns>
        /// <remarks>The native pipeline belongs to the encoder, so the managed pipeline is created once and reused.
        /// Effect components rely on this to detect a re-created media by a changed pipeline instance.</remarks>
        public MediaPipeline GetPipeline()
        {
            OdinLog.LogAssert(Handle.IsAlive, $"{nameof(GetPipeline)} {nameof(MediaEncoder)} handle is released");

            if (Handle.IsAlive == false) return null;
            if (Pipeline != null) return Pipeline;
            return Pipeline = new MediaPipeline(Odin.Library.Methods.EncoderGetPipeline(Handle));
        }

        /// <summary>
        /// Set native position in channel 
        /// </summary>
        /// <remarks>The encoder transmits on every channel it has a position for, see <see cref="ClearPosition"/> to stop transmitting on a channel.</remarks>
        /// <param name="mask">channels to set the position for</param>
        /// <param name="position">3d position</param>
        /// <returns>3d position</returns>
        public OdinPosition SetPosition(Utility.ChannelMask mask, OdinPosition position)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(SetPosition)} {nameof(MediaEncoder)} handle is released");

            if (Handle.IsAlive == false) return position;
            var result = Odin.Library.Methods.EncoderSetPosition(Handle, mask, position);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderSetPosition)} in {nameof(MediaEncoder.SetPosition)}: {Utility.OdinLastErrorString()} (code {result})");
            if (Utility.IsOk(result))
                ChannelMask |= mask;
            return Position = position;
        }

        /// <summary>
        /// Clear native position in channel
        /// </summary>
        /// <remarks>The encoder stops transmitting on the cleared channels until a position is set again with <see cref="SetPosition"/>.</remarks>
        /// <param name="mask">channels to clear the position of</param>
        /// <returns>true on success or false</returns>
        public bool ClearPosition(Utility.ChannelMask mask)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(ClearPosition)} {nameof(MediaEncoder)} handle is released");

            if (Handle.IsAlive == false) return false;
            var result = Odin.Library.Methods.EncoderClearPosition(Handle, mask);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderClearPosition)} in {nameof(MediaEncoder.ClearPosition)}: {Utility.OdinLastErrorString()} (code {result})");
            if (Utility.IsOk(result))
                ChannelMask &= ~mask;
            return Utility.IsOk(result);
        }

        /// <summary>
        /// Set active native channels
        /// </summary>
        /// <remarks>Sets the last <see cref="Position"/> for the channels in <paramref name="mask"/> and clears the position of all other channels.</remarks>
        /// <returns>channel mask</returns>
        public Utility.ChannelMask SetChannels(Utility.ChannelMask mask)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(SetChannels)} {nameof(MediaEncoder)} handle is released");

            if (Handle.IsAlive == false) return ChannelMask = Utility.ChannelMask.None;
            // channels outside of the mask would keep transmitting with their previous position
            Utility.ChannelMask removed = ChannelMask & ~mask;
            if (removed != Utility.ChannelMask.None)
                ClearPosition(removed);
            if (mask != Utility.ChannelMask.None)
                SetPosition(mask, Position);
            return ChannelMask;
        }

        /// <summary>
        /// This reflects the internal silence detection state of the encoder, which updates as audio is processed
        /// </summary>
        /// <remarks>Always returns true on invalid handle.</remarks>
        /// <returns>Returns whether the encoder is currently processing silence</returns>
        public bool GetIsSilent()
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(GetIsSilent)} {nameof(MediaEncoder)} handle is released");
            if (Handle.IsAlive == false) return true; // skip call since native result will always be: true

            return Odin.Library.Methods.EncoderIsSilent(Handle);
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

            OdinLog.Assert(error == OdinError.ODIN_ERROR_NO_DATA, $"{nameof(Odin.Library.Methods.EncoderPop)} in {nameof(MediaEncoder.PopAll)} unexpected: {Utility.OdinLastErrorString()} (code {error})");
            return error == OdinError.ODIN_ERROR_NO_DATA;
        }

        /// <summary>
        /// Pop one datagram from the media
        /// </summary>
        /// <param name="datagram"></param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/>/<see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_NO_DATA"/> as OK or error</returns>
        public OdinError Pop(out byte[] datagram)
        {
            datagram = new byte[1920];
            // keep the native encoder alive for the call, a concurrent Dispose
            // (e.g. room teardown) defers the free instead of pulling the handle
            bool refAdded = false;
            try { Handle.DangerousAddRef(ref refAdded); }
            catch (ObjectDisposedException) { return OdinError.ODIN_ERROR_CLOSED; }
            try
            {
                var result = Odin.Library.Methods.EncoderPop(Handle, ref datagram);
                if (OdinDefaults.DEBUG) OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderPop)} in {nameof(MediaEncoder.Pop)}: {Utility.OdinLastErrorString()} (code {result})", silent: true);
                return result;
            }
            finally
            {
                if (refAdded) Handle.DangerousRelease();
            }
        }

        internal OdinError Pop(byte[] buffer, out uint count)
        {
            count = 0;
            bool refAdded = false;
            try { Handle.DangerousAddRef(ref refAdded); }
            catch (ObjectDisposedException) { return OdinError.ODIN_ERROR_CLOSED; }
            try
            {
                return Odin.Library.Methods.EncoderPop(Handle, buffer, out count);
            }
            finally
            {
                if (refAdded) Handle.DangerousRelease();
            }
        }

        /// <summary>
        /// Push samples to the media
        /// </summary>
        /// <param name="samples">audio samples</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> as OK or error</returns>
        public OdinError Push(float[] samples)
        {
            // see Pop for the DangerousAddRef rationale
            bool refAdded = false;
            try { Handle.DangerousAddRef(ref refAdded); }
            catch (ObjectDisposedException) { return OdinError.ODIN_ERROR_CLOSED; }
            try
            {
                var result = Odin.Library.Methods.EncoderPush(Handle, samples);
                if (OdinDefaults.DEBUG) OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.EncoderPush)} in {nameof(MediaEncoder.Push)}: {Utility.OdinLastErrorString()} (code {result})", silent: true);
                return result;
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
                    // the native pipeline is freed together with the media, effect components
                    // holding the pipeline must see it as released instead of calling into it
                    Pipeline?.Handle?.Dispose();
                    Pipeline = null;
                    AddtionalData = null;
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
