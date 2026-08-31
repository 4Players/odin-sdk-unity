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
        /// Collection of managed <see cref="IPipelineEffect"/>
        /// </summary>
        private readonly LinkedList<IPipelineEffect> _Effects;
        private readonly object _EffectsLock = new object();

        public MediaPipeline(OdinPipelineHandle handle)
        {
            Handle = handle;
            _Effects = new LinkedList<IPipelineEffect>();
        }

        /// <summary>
        /// Get Pipeline effects collection
        /// </summary>
        /// <returns>Get effects mapped by id</returns>
        public ILookup<uint, IPipelineEffect> GetEffects() => _Effects.ToLookup(effect => effect.Id);

        public void InsertEffect(IPipelineEffect effect)
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
                        _Effects.AddAfter(current, new LinkedListNode<IPipelineEffect>(effect));
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
                foreach (IPipelineEffect item in GetEffects().SelectMany(kvp => kvp))
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
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.GetEffectCount)} handle is released");

            return Odin.Library.Methods.PipelineGetEffectCount(Handle);
        }

        /// <summary>
        /// Get the effect id of the native pipeline by index id
        /// </summary>
        /// <param name="indexId">index id</param>
        /// <returns>effect id</returns>
        public uint GetEffectId(uint indexId)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.GetEffectId)} handle is released");

            var result = Odin.Library.Methods.PipelineGetEffectId(Handle, indexId, out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineGetEffectId)} in {nameof(MediaPipeline.GetEffectId)}: {Utility.OdinLastErrorString()} (code {result})");

            return effectId;
        }

        /// <summary>
        /// Insert an APM effect and sets the apm config
        /// </summary>
        /// <param name="samplerate">effect playback samplerate</param>
        /// <param name="stereo">effect playback stereo</param>
        /// <param name="config">configuration settings for apm</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.ApmEffect"/> instance of base <see cref="PipelineEffect"/> : <see cref="IPipelineEffect"/></returns>
        public ApmEffect AddApmEffect(uint samplerate, bool stereo, OdinApmConfig config) => ApmEffect.Create(this, samplerate, stereo, config);
        /// <summary>
        /// Insert an APM effect to native pipeline
        /// </summary>
        /// <param name="indexId">effect index</param>
        /// <param name="samplerate">effect playback samplerate</param>
        /// <param name="stereo">effect playback stereo</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.ApmEffect"/></returns>
        public ApmEffect InsertApmEffect(uint indexId, uint samplerate, bool stereo)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertApmEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertApmEffect(Handle, indexId, samplerate, stereo, out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertApmEffect)} in {nameof(MediaPipeline.InsertApmEffect)}: {Utility.OdinLastErrorString()} (code {result})");

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
        /// <returns><see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/> instance of base <see cref="PipelineEffect"/> : <see cref="IPipelineEffect"/></returns>
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
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertCustomEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertCustomEffect(Handle, indexId, callback, user_data, out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertCustomEffect)} in {nameof(MediaPipeline.InsertCustomEffect)}: {Utility.OdinLastErrorString()} (code {result})");

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
        /// <param name="effect">created <see cref="OdinNative.Wrapper.Media.VadEffect"/> instance or null</param>
        /// <returns>true on success or false</returns>
        public bool AddVadEffect(out VadEffect effect) => VadEffect.Create(this, out effect);
        /// <summary>
        /// Insert a vad effect to native pipeline
        /// </summary>
        /// <param name="indexId">effect index</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.VadEffect"/></returns>
        public VadEffect InsertVadEffect(uint indexId)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertVadEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertVadEffect(Handle, indexId, out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertVadEffect)} in {nameof(MediaPipeline.InsertVadEffect)}: {Utility.OdinLastErrorString()} (code {result})");

            VadEffect effect = null;
            if (Utility.IsOk(result))
                InsertEffect(effect = new VadEffect(Handle, effectId) { Index = indexId });

            return effect;
        }

        /// <summary>
        /// Insert a vi effect
        /// </summary>
        /// <remarks>see <see cref="OdinNative.Wrapper.Media.ViEffect.SetViConfig(OdinViConfig)"/> for more configuration</remarks>
        /// <param name="effect">created <see cref="OdinNative.Wrapper.Media.ViEffect"/> instance or null</param>
        /// <returns>true on success or false</returns>
        public bool AddViEffect(out ViEffect effect) => ViEffect.Create(this, out effect);
        /// <summary>
        /// Insert a vi effect to native pipeline
        /// </summary>
        /// <param name="indexId">effect index</param>
        /// <returns><see cref="OdinNative.Wrapper.Media.ViEffect"/></returns>
        public ViEffect InsertViEffect(uint indexId)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.InsertViEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineInsertViEffect(Handle, indexId, out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertViEffect)} in {nameof(MediaPipeline.InsertViEffect)}: {Utility.OdinLastErrorString()} (code {result})");

            ViEffect effect = null;
            if (Utility.IsOk(result))
                InsertEffect(effect = new ViEffect(Handle, effectId) { Index = indexId });

            return effect;
        }

        /// <summary>
        /// Moves an effect in the native pipeline to a new index
        /// </summary>
        /// <param name="effectId">native effect id</param>
        /// <param name="oldIndexId">currently unused</param>
        /// <param name="newIndexId">native index id</param>
        /// <remarks>If adding the effect to <see cref="_Effects"/> fails, retry with a <paramref name="newIndexId"/> from <see cref="GetNextIndex"/></remarks>
        /// <returns>true on success or false</returns>
        public bool MoveEffect(uint effectId, uint oldIndexId, ref uint newIndexId)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.MoveEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineMoveEffect(Handle, effectId, newIndexId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineMoveEffect)} in {nameof(MediaPipeline.MoveEffect)}: {Utility.OdinLastErrorString()} (code {result})");

            if (Utility.IsOk(result))
                UpdateIndices();

            return Utility.IsOk(result);
        }

        /// <summary>
        /// Removes an effect from the native pipeline
        /// </summary>
        /// <param name="effectId">effect id</param>
        /// <returns>true on removed or false</returns>
        public bool RemoveEffect(uint effectId)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(MediaPipeline.RemoveEffect)} handle is released");

            var result = Odin.Library.Methods.PipelineRemoveEffect(Handle, effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineRemoveEffect)} in {nameof(MediaPipeline.RemoveEffect)}: {Utility.OdinLastErrorString()} (code {result})");

            if (Utility.IsOk(result))
            {
                lock (_EffectsLock)
                {
                    IPipelineEffect effect = _Effects.FirstOrDefault(e => e.Id == effectId);
                    if (effect != null)
                    {
                        _Effects.Remove(effect);
                        // effects like CustomActivityEffect hold a self-rooting GCHandle that
                        // native no longer references after the removal above
                        (effect as IDisposable)?.Dispose();
                    }
                }
                UpdateIndices();
            }

            return Utility.IsOk(result);
        }
    }
}