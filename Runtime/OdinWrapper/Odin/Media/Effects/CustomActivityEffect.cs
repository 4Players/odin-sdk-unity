using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Wrapper.Media
{
    /// <summary>
    /// Custom activity detection effect for <see cref="MediaPipeline"/> callbacks.
    /// <para>
    /// Observes the <c>isSilent</c> flag emitted by the native pipeline on each audio frame
    /// without modifying the audio data or the silence state itself.
    /// Subscribe to <see cref="OnActivityChanged"/> on the returned instance to receive per-frame silence state updates.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Callbacks are raised on the native audio thread.
    /// Marshal to the Unity main thread before using Unity APIs.
    /// </remarks>
    public class CustomActivityEffect : PipelineEffect, IDisposable
    {
        /// <summary>
        /// Delegate for silence state change notifications from the native pipeline.
        /// </summary>
        /// <param name="isSilent">true when the audio stream transitioned to silent</param>
        /// <param name="peerId">peer that owns the media stream, or 0 if not set</param>
        /// <param name="mediaId">media stream id, or 0 if not set</param>
        public delegate void ActivityChangedDelegate(bool isSilent, uint peerId, ulong mediaId);

        /// <summary>
        /// Raised when the silence state reported by the native pipeline changes.
        /// Subscribers must not modify the state. To silence audio use a dedicated mute effect instead.
        /// </summary>
        /// <remarks>Raised on the native audio thread.</remarks>
        public event ActivityChangedDelegate OnActivityChanged;

        /// <summary>
        /// Peer id passed through to <see cref="OnActivityChanged"/> subscribers.
        /// </summary>
        public uint PeerId { get; set; }
        /// <summary>
        /// Media stream id passed through to <see cref="OnActivityChanged"/> subscribers.
        /// </summary>
        public ulong MediaId { get; set; }

        private bool _lastIsSilent = true;
        private GCHandle _selfHandle;

        private static readonly OdinCustomEffectCallbackDelegate StaticCallbackDelegate = StaticCallback;

        // same as Room so IL2CPP can marshal
#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(OdinCustomEffectCallbackDelegate))]
#endif
        private static void StaticCallback(IntPtr bufferPtr, uint samplesCount, [In, Out][MarshalAs(UnmanagedType.I1)] ref bool isSilent, IntPtr userdata)
        {
            if (userdata == IntPtr.Zero) return;
            if (GCHandle.FromIntPtr(userdata).Target is not CustomActivityEffect effect) return;
            if (isSilent == effect._lastIsSilent) return;
            effect._lastIsSilent = isSilent;
            effect.OnActivityChanged?.Invoke(isSilent, effect.PeerId, effect.MediaId);
        }

        /// <summary>
        /// Internal effect constructor. Use <see cref="CustomActivityEffect.Create(MediaPipeline, uint, ulong)"/>.
        /// </summary>
        /// <param name="parentHandle">native pipeline handle</param>
        /// <param name="effectId">native effect id</param>
        protected internal CustomActivityEffect(OdinPipelineHandle parentHandle, uint effectId) : base(parentHandle, effectId) { }

        ~CustomActivityEffect()
        {
            if (_selfHandle.IsAllocated)
                _selfHandle.Free();
        }

        /// <summary>
        /// Frees the native callback handle. Must be called after removing the effect from its pipeline.
        /// </summary>
        public void Dispose()
        {
            if (_selfHandle.IsAllocated)
                _selfHandle.Free();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Insert a <see cref="CustomActivityEffect"/> into the specified pipeline.
        /// </summary>
        /// <param name="pipeline">pipeline to insert the effect into</param>
        /// <param name="peerId">peer id passed through to <see cref="OnActivityChanged"/></param>
        /// <param name="mediaId">media id passed through to <see cref="OnActivityChanged"/></param>
        /// <returns>instance of <see cref="CustomActivityEffect"/> or null on failure</returns>
        public static CustomActivityEffect Create(MediaPipeline pipeline, uint peerId = 0, ulong mediaId = 0)
        {
            if (pipeline == null) return null;

            CustomActivityEffect effect = new(pipeline.Handle, 0)
            {
                PeerId = peerId,
                MediaId = mediaId,
            };

            effect._selfHandle = GCHandle.Alloc(effect);

            uint newIndex = pipeline.GetNextIndex();
            var result = Odin.Library.Methods.PipelineInsertCustomEffect(pipeline.Handle, newIndex, StaticCallbackDelegate, GCHandle.ToIntPtr(effect._selfHandle), out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertCustomEffect)} in {nameof(CustomActivityEffect.Create)}: {Utility.OdinLastErrorString()} (code {result})");

            if (!Utility.IsOk(result))
            {
                effect._selfHandle.Free();
                return null;
            }

            effect.Callback = StaticCallbackDelegate;
            effect.Index = newIndex;
            effect.Id = effectId;
            pipeline.InsertEffect(effect);
            return effect;
        }
    }
}
