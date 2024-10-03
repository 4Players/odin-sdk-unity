using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Wrapper.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Odin audio pipeline
    /// </summary>
    public class MediaPipeline
    {
        /// <summary>
        /// Internal native pipeline handle
        /// </summary>
        internal OdinPipelineHandle Handle { get; private set; }

        /// <summary>
        /// Collection of managed <see cref="IPiplineEffect"/>
        /// </summary>
        private readonly LinkedList<IPiplineEffect> _Effects;
        private readonly object _EffectsLock = new object();

        public MediaPipeline(OdinPipelineHandle handle)
        {
            Handle = handle;
            _Effects = new LinkedList<IPiplineEffect>();
        }

        /// <summary>
        /// Get Pipeline effects collection
        /// </summary>
        /// <returns>Get effects mapped by id</returns>
        public ILookup<uint, IPiplineEffect> GetEffects() => _Effects.ToLookup(effect => effect.Id);

        void InsertEffect(IPiplineEffect effect)
        {
            lock (_EffectsLock)
            {
                if (_Effects.First == null)
                {
                    _Effects.AddFirst(effect);
                    return;
                }

                var current = _Effects.Last;
                while (current != null)
                {
                    var next = current.Previous;
                    if (current.Value.Index <= effect.Index)
                    {
                        _Effects.AddAfter(current, new LinkedListNode<IPiplineEffect>(effect));
                        break;
                    }
                    else
                        current = next;
                }

                UpdateIndices();
            }
        }

        void UpdateIndices()
        {
            lock (_EffectsLock)
            {
                foreach (IPiplineEffect item in GetEffects().SelectMany(kvp => kvp))
                {
                    if(Utility.IsOk(item.GetEffectIndex(out uint index)))
                        item.Index = index;
                    else
                        _Effects.Remove(item);
                }
            }
        }

        /// <summary>
        /// Calculate the next available index based on <see cref="_Effects"/> entries
        /// </summary>
        /// <returns>next available index</returns>
        public uint GetNextIndex() => GetEffectCount();


        /// <summary>
        /// Get the current effect count of the native pipeline
        /// </summary>
        /// <returns>count of native registered effects</returns>
        public uint GetEffectCount() {
            Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.GetEffectCount)} handle is released");

            return Odin.Library.Methods.PipelineGetEffectCount(Handle);
        }

        /// <summary>
        /// Get the effect id of the native pipeline by index id
        /// </summary>
        /// <param name="indexId">index id</param>
        /// <returns>effect id</returns>
        public uint GetEffectId(uint indexId)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.GetEffectId)} handle is released");

            var result = Odin.Library.Methods.PipelineGetEffectId(Handle, indexId, out uint effectId);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetEffectId)} in {nameof(MediaPipeline.GetEffectId)} failed: {Utility.OdinLastErrorString()} (code {result})");

            return effectId;
        }

        /// <summary>
        /// Insert a apm effect and sets the apm config
        /// </summary>
        /// <param name="samplerate">effect playback samplerate</param>
        /// <param name="stereo">effect playback stereo</param>
        /// <param name="config">configuration settings for apm</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.ApmEffect"/> instance of base <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> : <see cref="OdinNative.Wrapper.Media.IPiplineEffect"/></returns>
        public ApmEffect AddApmEffect(uint samplerate, bool stereo, OdinApmConfig config) => ApmEffect.Create(this, samplerate, stereo, config);
        /// <summary>
        /// Insert a apm effect to native pipeline
        /// </summary>
        /// <param name="indexId">effect index</param>
        /// <param name="samplerate">effect playback samplerate</param>
        /// <param name="stereo">effect playback stereo</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.ApmEffect"/></returns>
        public ApmEffect InsertApmEffect(uint indexId, uint samplerate, bool stereo)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertApmEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertApmEffect(Handle, indexId, samplerate, stereo, out uint effectId);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertApmEffect)} in {nameof(MediaPipeline.InsertApmEffect)} failed: {Utility.OdinLastErrorString()} (code {result})");

            ApmEffect effect = null;
            if (Utility.IsOk(result))
                InsertEffect(effect = new ApmEffect(Handle, effectId)
                {
                    Index = indexId,
                    Samplerate = samplerate,
                    IsStereo = stereo
                });

            return effect;
        }

        /// <summary>
        /// Insert a custom effect and sets the <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/> where <typeparamref name="T"/> marks Serialize/Deserialize structures
        /// </summary>
        /// <typeparam name="T">userdata structure</typeparam>
        /// <param name="callback">effect callback</param>
        /// <param name="userData">effect callback userdata of type <typeparamref name="T"/></param>
        /// <returns><see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/> instance of base <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> : <see cref="OdinNative.Wrapper.Media.IPiplineEffect"/></returns>
        public CustomEffect<T> AddCustomEffect<T>(PipelineCallback<T> callback, T userData) where T: unmanaged => CustomEffect<T>.Create(this, callback, userData);
        /// <summary>
        /// Insert a custom effect to native pipeline
        /// </summary>
        /// <typeparam name="T">userdata structure</typeparam>
        /// <param name="indexId">effect index</param>
        /// <param name="callback">effect callback</param>
        /// <param name="user_data">effect callback userdata</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/></returns>
        public CustomEffect<T> InsertCustomEffect<T>(uint indexId, OdinCustomEffectCallbackDelegate callback, IntPtr user_data) where T : unmanaged
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertCustomEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertCustomEffect(Handle, indexId, callback, user_data, out uint effectId);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertCustomEffect)} in {nameof(MediaPipeline.InsertCustomEffect)} failed: {Utility.OdinLastErrorString()} (code {result})");

            CustomEffect<T> effect = null;
            if (Utility.IsOk(result))
                InsertEffect(effect = new CustomEffect<T>(Handle, effectId)
                { 
                    Index = indexId,
                    Callback = callback
                });

            return effect;
        }

        /// <summary>
        /// Insert a vad effect
        /// </summary>
        /// <remarks>see <see cref="OdinNative.Wrapper.Media.VadEffect.SetVadConfig(OdinVadConfig)"/> for more configuration</remarks>
        /// <param name="effect">configuration settings for vad</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.VadEffect"/> instance of base <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> : <see cref="OdinNative.Wrapper.Media.IPiplineEffect"/></returns>
        public bool AddVadEffect(out VadEffect effect) => VadEffect.Create(this, out effect);
        /// <summary>
        /// Insert a vad effect to native pipeline
        /// </summary>
        /// <param name="indexId">effect index</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.VadEffect"/></returns>
        public VadEffect InsertVadEffect(uint indexId)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertVadEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertVadEffect(Handle, indexId, out uint effectId);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertVadEffect)} in {nameof(MediaPipeline.InsertVadEffect)} failed: {Utility.OdinLastErrorString()} (code {result})");

            VadEffect effect = null;
            if (Utility.IsOk(result))
                InsertEffect(effect = new VadEffect(Handle, effectId) { Index = indexId });

            return effect;
        }

        /// <summary>
        /// Moves a effect in the native pipeline to a new index
        /// </summary>
        /// <param name="effectId">native effect id</param>
        /// <param name="oldIndexId"><see cref="_Effects"/> index id</param>
        /// <param name="newIndexId">native index id</param>
        /// <remarks>If adding the effect to <see cref="_Effects"/> fails, retry with a <paramref name="newIndexId"/> from <see cref="GetNextIndex"/></remarks>
        public bool MoveEffect(uint effectId, uint oldIndexId, ref uint newIndexId)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.MoveEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineMoveEffect(Handle, effectId, newIndexId);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineMoveEffect)} in {nameof(MediaPipeline.MoveEffect)} failed: {Utility.OdinLastErrorString()} (code {result})");

            if (Utility.IsOk(result))
                UpdateIndices();

            return Utility.IsOk(result);
        }

        /// <summary>
        /// Removes a effect from the native pipeline
        /// </summary>
        /// <param name="effectId">effect id</param>
        /// <returns>true on removed or false</returns>
        public bool RemoveEffect(uint effectId)
        {
            if (OdinDefaults.Debug)
                Utility.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.RemoveEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineRemoveEffect(Handle, effectId);
            if (OdinDefaults.Verbose)
                Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineRemoveEffect)} in {nameof(MediaPipeline.RemoveEffect)} failed: {Utility.OdinLastErrorString()} (code {result})");

            if (Utility.IsOk(result))
                UpdateIndices();

            return Utility.IsOk(result);
        }
    }
}