#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OdinNative.Unity.UIEditor
{
    [CustomEditor(typeof(OdinNative.Unity.OdinCrypto))]
    public class OdinCryptoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Password") && EditorApplication.isPlaying)
            {
                ((OdinCrypto)target).ChangePassword(((OdinCrypto)target).InitialPassword);
                OdinNative.OdinLog.LogDebug($"Changed {((OdinCrypto)target).InitialPassword}");
            }

            if (GUILayout.Button("Clear Password") && EditorApplication.isPlaying)
            {
                ((OdinCrypto)target).ChangePassword(null);
                OdinNative.OdinLog.LogInfo($"Cleared cipher");
            }
            GUILayout.EndHorizontal();

        }
    }
}
#endif
