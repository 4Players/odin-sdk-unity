using OdinNative.Core;
using OdinNative.Unity;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// VoiceIsolation component for <see cref="OdinNative.Wrapper.Media.ViEffect"/>
    /// <para>
    /// This class provides configuration for the native implemented deep-learning voice isolation which
    /// separates speech from background sound in the capture signal.
    /// </para>
    /// </summary>
    /// <remarks>Autoset <see cref="OdinNative.Core.Imports.NativeBindings.OdinViConfig"/> for built-in VI</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinViComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Voice isolation")]
    public class OdinViComponent : MonoBehaviour, IOdinEffect
    {
        public IMedia Media { get; set; }

        public ViEffect Effect { get; private set; }
        public bool IsCreated { get; private set; } = false;
        private bool _warn = true;

        /// <summary>
        /// Indicates whether the effect will attenuate non-speech portions of the input audio signal
        /// </summary>
        public bool Enabled = OdinDefaults.VoiceIsolation;
        /// <summary>
        /// Maximum attenuation applied to non-speech in dB; values of `100` and above remove
        /// background sound entirely, while values close to `0` disable noise reduction.
        /// </summary>
        public float AttenuationLimitDb = OdinDefaults.VoiceIsolationAttenuationLimitDb;

        void Reset()
        {
            IsCreated = false;

            Enabled = OdinDefaults.VoiceIsolation;
            AttenuationLimitDb = OdinDefaults.VoiceIsolationAttenuationLimitDb;
        }

        void Start()
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
                OdinLog.LogInfo($"{gameObject.name} does not have a {nameof(OdinDecoder)} or {nameof(OdinEncoder)} to add {nameof(ViEffect)} for this {nameof(OdinViComponent)}");
                return;
            }
        }

        // pipeline instance the effect was created on; stable per media lifetime since the
        // wrapper caches it at media creation, so a rebuild yields a new instance (no ABA)
        private MediaPipeline _effectPipeline;

        void Update()
        {
            MediaPipeline pipeline = Media?.GetPipeline();
            // a rebuilt media (room recreation) brings a new pipeline and the old effect died with it
            if (IsCreated && ReferenceEquals(pipeline, _effectPipeline) == false)
                ClearEffect();

            if (IsCreated == false)
            {
                if (pipeline == null)
                {
                    if (_warn)
                    {
                        // no pipeline is a regular state, e.g. an encoder before the room is joined
                        OdinLog.LogInfo($"{gameObject.name} {nameof(OdinViComponent)} can not create/add {nameof(ViEffect)} without a pipeline");
                        _warn = false;
                    }
                    return;
                }

                if (pipeline.AddViEffect(out ViEffect vi))
                {
                    Effect = vi;
                    _effectPipeline = pipeline;
                    UpdateConfig(pipeline);
                    IsCreated = true;
                    OdinLog.LogInfo($"{gameObject.name} {nameof(OdinViComponent)} added {nameof(ViEffect)} (id {Effect.Id})");
                }
                else if (_warn)
                {
                    OdinLog.LogError($"{gameObject.name} {nameof(OdinViComponent)} error in {nameof(MediaPipeline.AddViEffect)}");
                }
                _warn = true;
            }
            else if (Effect != null)
            {
                // update config on change
                if (Effect.Enabled != Enabled ||
                    Effect.AttenuationLimitDb != AttenuationLimitDb)
                    UpdateConfig();
            }
        }

        public void UpdateConfig() => UpdateConfig(Media?.GetPipeline());
        public void UpdateConfig(MediaPipeline pipeline)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinViComponent)} {nameof(UpdateConfig)} {nameof(MediaPipeline)} is null");
            OdinLog.Assert(Effect != null, $"{nameof(OdinViComponent)} {nameof(UpdateConfig)} {nameof(ViEffect)} is null");

            if (Effect == null) return;
            // the config write must target the pipeline the effect was created on
            if (ReferenceEquals(pipeline, _effectPipeline) == false) return;

            Effect.Enabled = Enabled;
            Effect.AttenuationLimitDb = AttenuationLimitDb;
            Effect.UpdateEffectConfig();
        }

        private void ClearEffect()
        {
            // remove from the pipeline the effect actually lives on, if it is still alive
            if (Effect != null && _effectPipeline?.Handle?.IsAlive == true)
                _effectPipeline.RemoveEffect(Effect.Id);

            Effect = null;
            IsCreated = false;
            _effectPipeline = null;
        }

        public void ResetEffect() => ResetEffect(Media?.GetPipeline());
        public void ResetEffect(MediaPipeline pipeline)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinViComponent)} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");
            if (pipeline == null) return;

            ClearEffect();

            if (pipeline.AddViEffect(out ViEffect effect))
            {
                Effect = effect;
                _effectPipeline = pipeline;
                IsCreated = true;
                UpdateConfig(pipeline);
            }
        }

        void OnDestroy()
        {
            ClearEffect();
        }

        public T GetMedia<T>() where T : IMedia => (T)Media;
        public PipelineEffect GetEffect() => Effect;
    }
}
