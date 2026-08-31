#if UNITY_WEBGL
using System;
using UnityEngine;

namespace OdinNative.Unity
{
    /// <summary>
    /// Placeholder for the ODIN WebGL js-bridge.
    /// <para>
    /// The previous bridge was built against a removed protocol version and is not compatible
    /// with the current SDK. This stub keeps WebGL builds compiling until the new web SDK
    /// is available and the bridge is reimplemented against it.
    /// </para>
    /// </summary>
    /// <remarks>WebGL voice chat is currently not supported; operations like <see cref="Join"/> and <see cref="Leave"/> throw <see cref="NotSupportedException"/>, state properties return inert defaults.</remarks>
    public class OdinWebRoom : MonoBehaviour
    {
        internal const string NotSupportedMessage = "ODIN WebGL support is pending the new web SDK and currently not available.";

        /// <summary>
        /// Room name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Room join state
        /// </summary>
        public bool IsJoined => false;

        void Awake()
        {
            Debug.LogWarning($"{nameof(OdinWebRoom)}: {NotSupportedMessage}");
        }

        /// <summary>
        /// Join the room
        /// </summary>
        /// <param name="token">room token</param>
        /// <returns>never returns, always throws</returns>
        /// <exception cref="NotSupportedException">WebGL support is pending the new web SDK</exception>
        public bool Join(string token) => throw new NotSupportedException(NotSupportedMessage);

        /// <summary>
        /// Leave the room
        /// </summary>
        /// <exception cref="NotSupportedException">WebGL support is pending the new web SDK</exception>
        public void Leave() => throw new NotSupportedException(NotSupportedMessage);
    }
}
#endif
