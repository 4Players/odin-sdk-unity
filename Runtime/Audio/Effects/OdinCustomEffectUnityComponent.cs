using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom base component for <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>
    /// <para>
    /// This convenient class provides effect template with a passthrough of custom userdata on a callback.
    /// The abstract representation of a base custom effect is to simplify implementation of <see cref="OdinNative.Unity.Audio.IOdinEffect"/> as a Unity component.
    /// (see other predefined custom effects)
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/></remarks>
    public abstract class OdinCustomEffectUnityComponentBase<U> : MonoBehaviour, IOdinEffect where U : unmanaged
    {
        private IMedia _Media;
        /// <summary>
        /// Get Media
        /// </summary>
        /// <remarks>Will reset the effect if the media id has changed</remarks>
        public virtual IMedia Media { get => _Media; 
            set 
            { 
                if (_Media != null && (_Media.Id != value?.Id || _Media != value))
                    this.ResetEffect();

                _Media = value;
            } 
        }
        /// <summary>
        /// Get base effect
        /// </summary>
        public PiplineEffect Effect { get; private set; }
        /// <summary>
        /// Flag if the effect was added to the pipeline
        /// </summary>
        public bool IsCreated { get; private set; }
        /// <summary>
        /// Flag if the component is active or use <c>isActiveAndEnabled</c>
        /// </summary>
        public bool IsEnabled { get; private set; }

        protected readonly ConcurrentQueue<UnityAction> UnityQueue = new ConcurrentQueue<UnityAction>();

        [Tooltip("Automatically destory this component if an error occurred in the registered effect callback.")]
        public bool DestroyOnError = true;

        protected bool _warn = true;
        protected bool _corrupt = false;

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
                Media = this.gameObject.GetComponent<OdinMedia>();

            if (Media == null && _warn)
            {
                Debug.Log($"{gameObject.name} does not have a {nameof(OdinMedia)} to add {typeof(CustomEffect<>)} for this {this.GetType()}");
                return;
            }
        }

        protected virtual void Update()
        {
            if (_corrupt)
            {
                Debug.LogError($"{gameObject.name} {this.GetType()} callback {typeof(CustomEffect<>)} is in fatal state. {(DestroyOnError ? "This component will destroy itself!" : "This component will be disabled!")}");
                if (DestroyOnError)
                    Destroy(this);
                else
                    this.enabled = false;

                return;
            }

            if (IsCreated == false)
            {
                MediaPipeline pipeline = Media?.GetPipeline();
                if (pipeline == null)
                {
                    if (_warn)
                    {
                        Debug.LogWarning($"{gameObject.name} {this.GetType()} can not create/add {typeof(CustomEffect<>)} without a pipline");
                        _warn = false;
                    }
                    return;
                }

                var customEffect = pipeline.AddCustomEffect(CustomEffectCallback, GetEffectUserData());

                if (customEffect != null)
                {
                    Effect = customEffect;

                    IsCreated = true;
                    if (OdinDefaults.Verbose || OdinDefaults.Debug) Debug.Log($"{gameObject.name} {this.GetType()} added {Effect.GetType()} (id {Effect.Id})");
                }
                else if (_warn)
                {
                    Debug.LogError($"{gameObject.name} {this.GetType()} error in {nameof(MediaPipeline.AddCustomEffect)}");
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
        /// Get userdata used for the <see cref="CustomEffectCallback(OdinCallbackAudioData, U)"/>
        /// </summary>
        /// <returns>effect userdata</returns>
        public virtual U GetEffectUserData() => default(U);
        /// <summary>
        /// Callback delegate for the effect
        /// </summary>
        /// <param name="audio">audio data wrapper for copy buffer to native</param>
        public virtual void CustomEffectCallback(OdinArrayf audio, ref bool isSilent, U _)
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
                Debug.LogError($"{this.GetType()} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");
                return;
            }

            if (Effect != null)
                pipeline.RemoveEffect(Effect.Id);

            var effect = pipeline.AddCustomEffect(CustomEffectCallback, GetEffectUserData()); 
            if (effect != null)
                Effect = effect;
        }

        protected virtual void OnDestroy()
        {
            if (Media != null && Effect != null)
                Media.GetPipeline()?.RemoveEffect(Effect.Id);
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
        public virtual PiplineEffect GetEffect() => Effect;
    }
}