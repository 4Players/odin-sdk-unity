using OdinNative.Odin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OdinNative.Unity
{
    [Serializable]
    public class OdinUserData
    {
        public string name;
        public string seed;
        public string status;
        public int muted;
        public string renderer;
        public string platform;
        public string revision;
        public string version;
        public string buildno;

        public OdinUserData() : this("Unity Player", "online") { }
        public OdinUserData(string name, string status)
        {
            this.name = name;
            this.seed = SystemInfo.deviceUniqueIdentifier;
            this.status = status;
            this.muted = 0;
            this.renderer = Application.unityVersion;
            this.platform = string.Format("{0}/{1}", Application.platform, Application.unityVersion);
            this.revision = "0";
            this.version = Application.version;
            this.buildno = Application.version;
        }

        public UserData ToUserData()
        {
            return new UserData(this.ToJson());
        }

        public static OdinUserData FromUserData(UserData userData)
        {
            return JsonUtility.FromJson<OdinUserData>(userData.ToString());
        }

        public static bool FromUserData(UserData userData, out OdinUserData odinUserData)
        {
            try
            {
                odinUserData = JsonUtility.FromJson<OdinUserData>(userData.ToString());
                return true;
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                odinUserData = null;
                return false;
            }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>avatars.dicebear url</returns>
        internal string GetAvatarUrl()
        {
            return string.Format("https://avatars.dicebear.com/api/bottts/{0}.svg?textureChance=0", this.seed);
        }
    }
}
