using OdinNative.Unity;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom base component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This convenient class provides effect template with a passthrough of custom userdata on a callback.
    /// The abstract representation of a base custom effect is to simplify implementation of <see cref="OdinNative.Unity.Audio.IOdinEffect"/> as a Unity component.
    /// (see other predefined custom effects)
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/></remarks>
    public abstract class OdinCustomEffectUnityComponentBase<U> : MonoBehaviour, IOdinEffect where U : unmanaged
    {
        private IMedia _Media;
        /// <summary>
        /// Get Media
        /// </summary>
        /// <remarks>Removes the effect from the previous media if the media has changed, it is added to the new media on the next update</remarks>
        public virtual IMedia Media { get => _Media; 
            set 
            { 
                if (_Media != null && (_Media.Id != value?.Id || _Media != value))
                    this.ClearEffect();

                _Media = value;
            } 
        }
        /// <summary>
        /// Get base effect
        /// </summary>
        public PipelineEffect Effect { get; private set; }
        /// <summary>
        /// Flag if the effect was added to the pipeline
        /// </summary>
        public bool IsCreated { get; private set; }
        /// <summary>
        /// Flag if the component is active or use <c>isActiveAndEnabled</c>
        /// </summary>
        public bool IsEnabled { get; private set; }

        protected readonly ConcurrentQueue<UnityAction> UnityQueue = new ConcurrentQueue<UnityAction>();

        [Tooltip("Automatically destroy this component if an error occurred in the registered effect callback.")]
        public bool DestroyOnError = true;

        protected bool _warn = true;
        protected bool _corrupt = false;

        // pipeline instance the effect was created on; stable per media lifetime since the
        // wrapper caches it at media creation, so a rebuild yields a new instance (no ABA)
        private MediaPipeline _effectPipeline;

        protected virtual void OnEnable()
        {
            IsEnabled = true;
        }

        protected virtual void Reset()
        {
            IsCreated = false;
            IsEnabled = isActiveAndEnabled;
            DestroyOnError = true;
        }

        protected virtual void Start()
        {
            if (Media == null)
            {
                if (this.gameObject.TryGetComponent(out OdinDecoder decoder))
                    Media = decoder;
                else if (this.gameObject.TryGetComponent(out OdinEncoder encoder))
                    Media = encoder;
            }

            if (Media == null && _warn)
            {
                OdinLog.LogInfo($"{gameObject.name} does not have a {nameof(OdinDecoder)} or {nameof(OdinEncoder)} to add {typeof(CustomEffect<>)} for this {this.GetType()}");
                return;
            }
        }

        protected virtual void Update()
        {
            if (_corrupt)
            {
                OdinLog.LogError($"{gameObject.name} {this.GetType()} callback {typeof(CustomEffect<>)} is in fatal state. {(DestroyOnError ? "This component will destroy itself!" : "This component will be disabled!")}");
                if (DestroyOnError)
                    Destroy(this);
                else
                    this.enabled = false;

                return;
            }

            MediaPipeline pipeline = Media?.GetPipeline();
            // a rebuilt media (e.g. OdinEncoder re-created after a room rejoin or a disable/enable cycle)
            // brings a new pipeline and the old effect died with it
            if (IsCreated && ReferenceEquals(pipeline, _effectPipeline) == false)
                ClearEffect();

            if (IsCreated == false)
            {
                if (pipeline == null)
                {
                    if (_warn)
                    {
                        OdinLog.LogInfo($"{gameObject.name} {this.GetType()} can not create/add {typeof(CustomEffect<>)} without a pipeline");
                        _warn = false;
                    }
                    return;
                }

                var customEffect = pipeline.AddCustomEffect(CustomEffectCallback, GetEffectUserData());

                if (customEffect != null)
                {
                    Effect = customEffect;
                    _effectPipeline = pipeline;

                    IsCreated = true;
                    OdinLog.LogInfo($"{gameObject.name} {this.GetType()} added {Effect.GetType()} (id {Effect.Id})");
                }
                else if (_warn)
                {
                    OdinLog.LogError($"{gameObject.name} {this.GetType()} error in {nameof(MediaPipeline.AddCustomEffect)}");
                }
                _warn = true;
            }

            if (UnityQueue.IsEmpty == false)
                while (UnityQueue.TryDequeue(out var action))
                    action?.Invoke();
        }

        protected virtual void OnDisable()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// Get userdata used for the <see cref="CustomEffectCallback(OdinTArray{float}, ref bool, U)"/>
        /// </summary>
        /// <returns>effect userdata</returns>
        public virtual U GetEffectUserData() => default(U);
        /// <summary>
        /// Callback delegate for the effect
        /// </summary>
        /// <param name="audio">audio data wrapper for copy buffer to native</param>
        /// <param name="isSilent">flag whether the buffer contains silence, can be set to mute the frame</param>
        /// <param name="_">effect userdata, see <see cref="GetEffectUserData"/></param>
        public virtual void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, U _)
        {
            if (_corrupt) return;
            if (!IsEnabled) return;
            if (audio == null)
            {
                _corrupt = true;
                return;
            }
        }

        /// <summary>
        /// Removes an effect from the pipeline and add a new effect with callbacks
        /// </summary>
        /// <remarks>This will affect the location of the effect in the pipeline by index</remarks>
        public virtual void ResetEffect()
        {
            var pipeline = Media?.GetPipeline();
            if (pipeline == null)
            {
                OdinLog.LogError($"{this.GetType()} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");
                return;
            }

            ClearEffect();

            var effect = pipeline.AddCustomEffect(CustomEffectCallback, GetEffectUserData()); 
            if (effect != null)
            {
                Effect = effect;
                _effectPipeline = pipeline;
                IsCreated = true;
            }
        }

        /// <summary>
        /// Removes the effect from the pipeline it was created on, if that pipeline is still alive
        /// </summary>
        /// <remarks>The effect is added again on the next update while a pipeline is available</remarks>
        protected virtual void ClearEffect()
        {
            if (Effect != null && _effectPipeline?.Handle?.IsAlive == true)
                _effectPipeline.RemoveEffect(Effect.Id);

            Effect = null;
            IsCreated = false;
            _effectPipeline = null;
        }

        protected virtual void OnDestroy()
        {
            ClearEffect();
        }

        /// <summary>
        /// Get Media
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Media</returns>
        public virtual T GetMedia<T>() where T : IMedia => (T)Media;
        /// <summary>
        /// Get custom effect
        /// </summary>
        /// <returns>custom effect</returns>
        public virtual PipelineEffect GetEffect() => Effect;
    }
}