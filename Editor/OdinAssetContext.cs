#if UNITY_EDITOR
using OdinNative.Unity;
using OdinNative.Unity.Audio;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public class OdinAssetContext : Editor
{
    internal const string MenuTag = "Assets/Create/4Players ODIN";
    internal const string MenuEditorTag = MenuTag + "/OdinEditor Helper";
    internal const string ContextTag = "Audio/4Players ODIN";
    private static GameObject NewOdinInstance(string guid = "f7d24aee5ad24a646a7b72d963a24b6a")
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (!assetPath.EndsWith("OdinInstance.prefab"))
        {
            OdinNative.OdinLog.LogError($"OdinInstance.prefab not found! Resolved asset path: \"{assetPath}\"");
            return null;
        }

        Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
        return PrefabUtility.InstantiatePrefab(asset) as GameObject;
    }

    [MenuItem(MenuEditorTag + "/Prefabs/Room")]
    public static GameObject CreateDefault()
    {
        GameObject odin = NewOdinInstance();
        if(odin == null) return null;

        return odin;
    }

    [ContextMenu("OdinRoom")]
    public OdinRoom CreateRoom()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            OdinNative.OdinLog.LogError($"Selection must be a gameobject!");
            return null;
        }
        return selected.AddComponent<OdinRoom>();
    }

    [MenuItem(MenuEditorTag + "/Template/Advanced (Room, Mic, MixerGroup)")]
    public static GameObject CreateAdvanced()
    {
        OdinNative.OdinLog.LogInfo($"Create Prefab...");
        GameObject odin = CreateDefault();
        if (odin == null) return null;

        OdinRoom handler = odin.GetComponent<OdinRoom>();
        if (handler != null)
        {
            Object obj = Selection.activeObject;
            OdinNative.OdinLog.LogInfo("Set playback mixer settings...");
            if (obj is AudioMixer)
            {
                AudioMixer mixer = obj as AudioMixer;
                OdinNative.OdinLog.LogInfo("Looking for \"Odin\"-group...");
                var groups = mixer.FindMatchingGroups("Odin");
                if (groups.Length <= 0)
                {
                    OdinNative.OdinLog.LogInfo("Looking for \"Master\"-group...");
                    groups = mixer.FindMatchingGroups("Master");
                }
                handler.AudioMixerGroup = groups.FirstOrDefault();
            }
            else if (obj is AudioMixerGroup)
            {
                AudioMixerGroup group = obj as AudioMixerGroup;
                OdinNative.OdinLog.LogInfo($"Set \"{group.name}\"-group...");
                handler.AudioMixerGroup = group;
            }
            else
                OdinNative.OdinLog.LogWarning("Selection has to be an AudioMixer or AudioMixerGroup! Skip playback mixer settings.");

            var mic = odin.AddComponent<OdinMicrophoneReader>();
            if (mic.OnAudioData == null) mic.OnAudioData = new OdinNative.Unity.Events.UnityAudioData();
            if (mic.OnAudioData.GetPersistentEventCount() <= 0)
            {
#if UNITY_EDITOR
                UnityEditor.Events.UnityEventTools.AddPersistentListener(mic.OnAudioData, handler.ProxyAudio);
#else
                mic.OnAudioData.AddListener(handler.ProxyAudio);
#endif
            }
            else
                OdinNative.OdinLog.LogWarning("Persistent event is already set! Skip microphone reader listener.");
        }

        return odin;
    }

    [MenuItem(MenuEditorTag + "/Components/Basic (OdinRoom)")]
    public static GameObject CreateComponents()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.transform.parent != null)
        {
            OdinNative.OdinLog.LogError("Selected object has to be a root GameObject!");
            return null;
        }

        OdinRoom[] rooms = FindObjectsByType<OdinRoom>(FindObjectsSortMode.None);
        OdinRoom room = rooms.Length <= 0 ? obj.AddComponent<OdinRoom>() : rooms[0];

        return obj;
    }

    [MenuItem(MenuEditorTag + "/Components/Extended (Room, Microphone)")]
    public static GameObject CreateFullComponents()
    {
        GameObject obj = CreateComponents();
        if (obj == null) return null; // invalid selection, already logged

        OdinRoom handler = obj.GetComponent<OdinRoom>();
        OdinMicrophoneReader[] micReaders = FindObjectsByType<OdinMicrophoneReader>(FindObjectsSortMode.None);
        if (handler != null)
        {
            var mic = micReaders.Length <= 0 ? obj.AddComponent<OdinMicrophoneReader>() : micReaders[0];
            if (mic.OnAudioData == null) mic.OnAudioData = new OdinNative.Unity.Events.UnityAudioData();
            if (mic.OnAudioData.GetPersistentEventCount() > 0)
            {
                OdinNative.OdinLog.LogWarning("Skip listener! Persistent event is already set.");
                return obj;
            }

#if UNITY_EDITOR
            UnityEditor.Events.UnityEventTools.AddPersistentListener(mic.OnAudioData, handler.ProxyAudio);
#else
            mic.OnAudioData.AddListener(handler.ProxyAudio);
#endif
        }

        return obj;
    }

    [MenuItem(MenuTag + "/Link AudioMixerGroup to OdinRoom")]
    public static void LinkAudioMixerGroup()
    {
        Object obj = Selection.activeObject;
        foreach (OdinRoom handler in FindObjectsByType<OdinRoom>(FindObjectsSortMode.None))
        {
            if (obj is AudioMixerGroup)
            {
                AudioMixerGroup group = obj as AudioMixerGroup;
                handler.AudioMixerGroup = group;
            }
            else
            {
                OdinNative.OdinLog.LogWarning("Selection has to be an AudioMixerGroup!");
                break;
            }
        }
    }
}
#endif