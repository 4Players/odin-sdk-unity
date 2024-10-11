using OdinNative.Odin;
using System;
using System.Text;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
    /// <summary>
    /// Custom user data class for serializing Odin peer user information in a JSON format.
    /// Implements the IUserData interface.
    /// </summary>
    [Serializable]
    class CustomUserDataJsonFormat : IUserData
    {
        public string id; // Unique identifier for the device.
        public string seed; // Used for generating avatars.
        public string name; // User's display name.
        public string status; // User's status (e.g., online, offline).
        public int inputMuted; // Input mute state (0 = unmuted, 1 = muted).
        public int outputMuted; // Output mute state (0 = unmuted, 1 = muted).
        public string renderer; // Renderer version (usually Unity version).
        public string revision; // Application revision number.
        public string version; // Application version.
        public string platform; // Platform and Unity version.
        public string buildno; // Application build number.

        /// <summary>
        /// Default constructor initializing with default name and status.
        /// </summary>
        public CustomUserDataJsonFormat() : this("Unity Player", "online") { }
        
        /// <summary>
        /// Constructor for initializing custom user data.
        /// </summary>
        /// <param name="name">User's name.</param>
        /// <param name="status">User's online status.</param>
        public CustomUserDataJsonFormat(string name, string status)
        {
            this.id = SystemInfo.deviceUniqueIdentifier;
            this.seed = SystemInfo.deviceUniqueIdentifier;
            this.name = name;
            this.status = status;
            this.inputMuted = 0;
            this.outputMuted = 0;
            this.renderer = Application.unityVersion;
            this.revision = "0";
            this.version = Application.version;
            this.platform = string.Format("{0}/{1}", Application.platform, Application.unityVersion);
            this.buildno = Application.buildGUID;
        }

        /// <summary>
        /// Converts the current instance to a UserData object.
        /// </summary>
        /// <returns>A UserData instance containing the serialized JSON.</returns>
        public UserData ToUserData()
        {
            return new UserData(this.ToJson());
        }

        /// <summary>
        /// Creates a CustomUserDataJsonFormat object from IUserData.
        /// </summary>
        /// <param name="userData">The IUserData instance to parse.</param>
        /// <returns>A CustomUserDataJsonFormat object, or null if parsing fails.</returns>
        public static CustomUserDataJsonFormat FromUserData(IUserData userData)
        {
            try
            {
                return JsonUtility.FromJson<CustomUserDataJsonFormat>(userData.ToString());
            } catch { return null; }
        }

        /// <summary>
        /// Try to parse a UserData object into a CustomUserDataJsonFormat object.
        /// </summary>
        /// <param name="userData">The UserData to parse.</param>
        /// <param name="customUserData">The parsed custom user data</param>
        /// <returns>True if parsing is successful, false otherwise.</returns>
        public static bool FromUserData(UserData userData, out CustomUserDataJsonFormat customUserData)
        {
            try
            {
                customUserData = JsonUtility.FromJson<CustomUserDataJsonFormat>(userData.ToString());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                customUserData = null;
                return false;
            }
        }

        /// <summary>
        /// Generates a URL for the user's avatar based on the seed.
        /// </summary>
        /// <returns>A string containing the avatar URL.</returns>
        internal string GetAvatarUrl()
        {
            return string.Format("https://avatars.dicebear.com/api/bottts/{0}.svg?textureChance=0", this.seed);
        }

        /// <summary>
        /// Converts the current instance to a JSON string.
        /// </summary>
        /// <returns>A JSON string representing the user data.</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Converts the current instance to a string in JSON format.
        /// </summary>
        /// <returns>A JSON string representing the user data.</returns>
        public override string ToString()
        {
            return this.ToJson();
        }

        /// <summary>
        /// Convert to a byte array.
        /// </summary>
        /// <returns>A byte array of the JSON string representation.</returns>
        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(this.ToString());
        }

        /// <summary>
        /// Checks if the user data is empty.
        /// </summary>
        /// <returns>True if the user data is empty, false otherwise.</returns>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.ToString());
        }
    }
}