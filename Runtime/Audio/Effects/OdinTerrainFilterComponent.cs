using System.Runtime.InteropServices;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Unity.Audio.OdinTerrainFilterComponent;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom filter by terrain component for <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>
    /// <para>
    /// This class is a effect in the odin audio pipline to mute audio based on location of a GameObject relative to a terrain type in Unity space.
    /// Note that the default implementation is Unity specific on a 2D calculation with X, Z and is not useful in all occlusions.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>; Odin supports a virtual position for Server-side culling (see <see cref="OdinNative.Wrapper.Room.Room"/>) outside of these pipline effects.</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinterrainfiltercomponent/")]
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
        public override void CustomEffectCallback(OdinArrayf audio, ref bool isSilent, TerrainFilterUserData userData)
        {
            base.CustomEffectCallback(audio, ref isSilent, userData);

            if (!base._corrupt && base.IsEnabled)
            {
                int chunkW = (int)userData.X; // Width
                int chunkL = (int)userData.Y; // Level
                int chunkH = (int)userData.Z; // Height

                // set flag if outside 2D alphamap 
                if ((chunkW > 1 || chunkW < 0) || (chunkH > 1 || chunkH < 0)) // ignore depth Level
                {
                    isSilent = true;
                }
            }
        }
    }
}