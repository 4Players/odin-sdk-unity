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
                Debug.Log($"Changed {((OdinCrypto)target).InitialPassword}");
            }

            if (GUILayout.Button("Clear Password") && EditorApplication.isPlaying)
            {
                ((OdinCrypto)target).ChangePassword(null);
                Debug.Log($"Cleared chipher");
            }
            GUILayout.EndHorizontal();

        }
    }
}
#endif
