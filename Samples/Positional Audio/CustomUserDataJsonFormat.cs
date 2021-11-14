using OdinNative.Odin;
using System;
using System.Text;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
    [Serializable]
    class CustomUserDataJsonFormat : IUserData
    {
        public string name;
        public string seed;
        public string status;
        public int muted;
        public string user;
        public string renderer;
        public string platform;
        public string revision;
        public string version;
        public string buildno;

        public CustomUserDataJsonFormat() : this("Unity Player", "online") { }
        public CustomUserDataJsonFormat(string name, string status)
        {
            this.name = name;
            this.seed = SystemInfo.deviceUniqueIdentifier;
            this.status = status;
            this.muted = 0;
            this.user = string.Format("{0}.{1}", Application.companyName, Application.productName);
            this.renderer = Application.unityVersion;
            this.platform = string.Format("{0}/{1}", Application.platform, Application.unityVersion);
            this.revision = "0";
            this.version = Application.version;
            this.buildno = Application.buildGUID;
        }

        public UserData ToUserData()
        {
            return new UserData(this.ToJson());
        }

        public static CustomUserDataJsonFormat FromUserData(UserData userData)
        {
            return JsonUtility.FromJson<CustomUserDataJsonFormat>(userData.ToString());
        }

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

        internal string GetAvatarUrl()
        {
            return string.Format("https://avatars.dicebear.com/api/bottts/{0}.svg?textureChance=0", this.seed);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(this.ToString());
        }
    }
}