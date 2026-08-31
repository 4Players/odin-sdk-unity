using System.Runtime.InteropServices;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Unity.Audio.OdinTerrainFilterComponent;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom filter by terrain component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This class is an effect in the odin audio pipeline to mute audio based on location of a GameObject relative to a terrain type in Unity space.
    /// Note that the default implementation is Unity specific on a 2D calculation with X, Z and is not useful in all occasions.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>; Odin supports a virtual position for Server-side culling (see <see cref="OdinNative.Wrapper.Room.Room"/>) outside of these pipeline effects.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinTerrainFilterComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Terrain Filter")]
    public class OdinTerrainFilterComponent : OdinCustomEffectUnityComponentBase<TerrainFilterUserData>
    {
        [Tooltip("Set the object to use for position determination")]
        public GameObject Emitter;
        [Tooltip("Set the terrain to use for positional alphamap calculation")]
        public Terrain Data;

        [StructLayout(LayoutKind.Sequential)]
        public struct TerrainFilterUserData 
        { 
            /// <summary>
            /// Original source
            /// </summary>
            public Vector3 ObjectPosition;
            /// <summary>
            /// Original terrain
            /// </summary>
            public Vector3 TerrainPosition;
            /// <summary>
            /// Relative position
            /// </summary>
            public Vector3 MapPosition;
            /// <summary>
            /// relative position to alphamap width
            /// </summary>
            public float X;
            /// <summary>
            /// relative position to alphamap layers
            /// </summary>
            public float Y;
            /// <summary>
            /// relative position to alphamap height
            /// </summary>
            public float Z;
        }
        protected TerrainFilterUserData _UserData = new TerrainFilterUserData();
        // updated on the main thread in Update, read by the effect callback on the audio thread
        protected volatile bool _OutsideTerrain;

        /// <summary>
        /// Set delegate userdata for effect callback
        /// </summary>
        /// <param name="objectPosition"></param>
        /// <param name="terrainPosition"></param>
        /// <param name="terrainData"></param>
        public virtual void SetUserData(Vector3 objectPosition, Vector3 terrainPosition, TerrainData terrainData)
        {
            _UserData.ObjectPosition = objectPosition;
            _UserData.TerrainPosition = terrainPosition;
            Vector3 relativePosition = objectPosition - terrainPosition;
            _UserData.MapPosition = new Vector3(
                terrainData.size.x == 0 ? 0 : relativePosition.x / terrainData.size.x,
                terrainData.size.y == 0 ? 0 : relativePosition.y / terrainData.size.y,
                terrainData.size.z == 0 ? 0 : relativePosition.z / terrainData.size.z );
            _UserData.X = _UserData.MapPosition.x * terrainData.alphamapWidth;
            _UserData.Y = _UserData.MapPosition.y * terrainData.alphamapLayers;
            _UserData.Z = _UserData.MapPosition.z * terrainData.alphamapHeight;
        }

        /// <summary>
        /// Get delegate userdata
        /// </summary>
        /// <returns>effect userdata</returns>
        public override TerrainFilterUserData GetEffectUserData()
        {
            if(Emitter != null && Data != null)
                SetUserData(Emitter.transform.position, Data.transform.position, Data.terrainData);

            return _UserData;
        }
        protected override void Update()
        {
            base.Update();

            if (Emitter != null && Data != null)
            {
                SetUserData(Emitter.transform.position, Data.transform.position, Data.terrainData);
                // set flag if outside the 2D terrain bounds (normalized map position, ignore depth Level)
                _OutsideTerrain = _UserData.MapPosition.x < 0f || _UserData.MapPosition.x > 1f
                    || _UserData.MapPosition.z < 0f || _UserData.MapPosition.z > 1f;
            }
            else
                _OutsideTerrain = false; // without references do not mute instead of keeping a stale flag
        }

        public override void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, TerrainFilterUserData userData)
        {
            base.CustomEffectCallback(audio, ref isSilent, userData);

            if (!base._corrupt && base.IsEnabled && _OutsideTerrain)
                isSilent = true;
        }
    }
}