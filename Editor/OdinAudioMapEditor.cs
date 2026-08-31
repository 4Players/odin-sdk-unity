#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OdinNative.Unity.UIEditor
{
    [CustomEditor(typeof(OdinAudioMap))]
    public class OdinAudioMapEditor : Editor
    {
        static readonly string[] typeOptions = new string[] { "Encoder", "Decoder" };
        bool showPosition = true;

        public override void OnInspectorGUI()
        {
            var map = (OdinAudioMap)target;
            serializedObject.Update();

            SerializedProperty nameProperty = serializedObject.FindProperty(nameof(OdinAudioMap.Name));
            SerializedProperty selectorProperty = serializedObject.FindProperty(nameof(OdinAudioMap.MapSelector));
            SerializedProperty valuesProperty = serializedObject.FindProperty(nameof(OdinAudioMap.ChannelValues));

            EditorGUILayout.PropertyField(nameProperty);
            selectorProperty.intValue = EditorGUILayout.MaskField($"Codec ({selectorProperty.intValue})", selectorProperty.intValue, typeOptions);

            showPosition = EditorGUILayout.Foldout(showPosition, "Channels Mask");
            if (showPosition)
            {
                string[] channelNames = map.ChannelKeys;
                for (int i = 0; i < channelNames.Length && i < valuesProperty.arraySize; i++)
                {
                    SerializedProperty element = valuesProperty.GetArrayElementAtIndex(i);
                    element.boolValue = EditorGUILayout.ToggleLeft(channelNames[i], element.boolValue);
                }
            }

            // changes go through the serialized object so undo and scene dirtying work;
            // apply before computing the mask display so it reflects this repaint's edits
            serializedObject.ApplyModifiedProperties();

            if (showPosition)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Channels: {map.Channels}");
                EditorGUILayout.LabelField($"Value: 0x{(ulong)map.ChannelMask:X}");
            }
        }
    }
}
#endif
