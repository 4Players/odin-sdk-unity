#if UNITY_EDITOR
using OdinNative.Unity;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds a custom layout to the OdinHandler component
/// </summary>
[CustomEditor(typeof(OdinHandler))]
public class OdinHandlerEditor : Editor
{
    SerializedProperty MicrophoneObject;

    SerializedProperty OnRoomJoin;
    SerializedProperty OnRoomJoined;
    SerializedProperty OnRoomLeave;
    SerializedProperty OnRoomLeft;
    SerializedProperty OnPeerJoined;
    SerializedProperty OnPeerUpdated;
    SerializedProperty OnPeerLeft;
    SerializedProperty OnMediaAdded;
    SerializedProperty OnMediaRemoved;

    SerializedProperty Persistent;
    SerializedProperty Audio3D;
    SerializedProperty PlaybackCreation;
    SerializedProperty AudioMixerObject;

    private bool toggleEventListeners;
    private bool toggleHandlerSettings;

    private GUIStyle FoldoutLabelStyle;
    private GUIStyle ToolbarTabStyle;
    int eventToolbarInt = 0;
    int roomEventToolbarInt = 0;
    string[] eventToolbarLabels = { "Peer joined", "Peer left", "Peer updated", "Media added", "Media removed" };
    string[] roomEventToolbarLabels = { "Room join", "Room joined", "Room leave", "Room left" };

    void OnEnable()
    {
        MicrophoneObject = serializedObject.FindProperty("Microphone");

        OnRoomJoin = serializedObject.FindProperty("OnRoomJoin");
        OnRoomJoined = serializedObject.FindProperty("OnRoomJoined");
        OnRoomLeave = serializedObject.FindProperty("OnRoomLeave");
        OnRoomLeft = serializedObject.FindProperty("OnRoomLeft");

        OnPeerJoined = serializedObject.FindProperty("OnPeerJoined");
        OnPeerUpdated = serializedObject.FindProperty("OnPeerUpdated");
        OnPeerLeft = serializedObject.FindProperty("OnPeerLeft");
        OnMediaAdded = serializedObject.FindProperty("OnMediaAdded");
        OnMediaRemoved = serializedObject.FindProperty("OnMediaRemoved");

        Persistent = serializedObject.FindProperty("_persistent");
        Audio3D = serializedObject.FindProperty("Use3DAudio");
        PlaybackCreation = serializedObject.FindProperty("CreatePlayback");
        AudioMixerObject = serializedObject.FindProperty("PlaybackAudioMixerGroup");
    }

    /// <summary>
    /// Implementation for the Unity custom inspector of OdinHandler.
    /// </summary>
    public override void OnInspectorGUI()
    {
        changeStyles();
        OdinHandler odinHandler = (target as OdinHandler);
        if (odinHandler == null)
        {
            DrawDefaultInspector(); // fallback
            return;
        }

        EditorGUILayout.PropertyField(MicrophoneObject, new GUIContent("Microphone", "Odin Microphone object"));
        GUILayout.Space(10);
        CreateEventListenersLayout();
        GUILayout.Space(10);
        CreateHandlerSettingsLayout();
        serializedObject.ApplyModifiedProperties();

    }

    private void changeStyles()
    {
        FoldoutLabelStyle = new GUIStyle(EditorStyles.foldout);
        FoldoutLabelStyle.fontStyle = FontStyle.Bold;
        FoldoutLabelStyle.fontSize = 14;

        ToolbarTabStyle = new GUIStyle(EditorStyles.toolbarButton);
    }

    private static void drawRect(int height)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height);
        rect.height = height;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        GUILayout.Space(3);

    }

    #region EventListeners
    private void CreateEventListenersLayout()
    {
        OdinHandler odinHandler = (target as OdinHandler);
        if (odinHandler == null)
            return; // skip Events

        toggleEventListeners = EditorGUILayout.Foldout(toggleEventListeners, "Events", FoldoutLabelStyle);
        drawRect(2);
        if (toggleEventListeners)
        {
            #region RoomEvents
            roomEventToolbarInt = GUILayout.Toolbar(roomEventToolbarInt, roomEventToolbarLabels, ToolbarTabStyle, GUI.ToolbarButtonSize.FitToContents);
            switch (roomEventToolbarInt)
            {
                // Room join
                case 0:
                    {
                        EditorGUILayout.PropertyField(OnRoomJoin, new GUIContent("OnRoomJoin", "Setup the room join event"));
                        break;
                    }
                // Room joined
                case 1:
                    {
                        EditorGUILayout.PropertyField(OnRoomJoined, new GUIContent("OnRoomJoined", "Setup the room joined event"));
                        break;
                    }
                // Room leave
                case 2:
                    {
                        EditorGUILayout.PropertyField(OnRoomLeave, new GUIContent("OnRoomLeave", "Setup the room leave event"));
                        break;
                    }
                // Room left
                case 3:
                    {
                        EditorGUILayout.PropertyField(OnRoomLeft, new GUIContent("OnRoomLeft", "Setup the room left event"));
                        break;
                    }
            }
            #endregion RoomEvents
            drawRect(1);
            #region SubRoomEvents
            eventToolbarInt = GUILayout.Toolbar(eventToolbarInt, eventToolbarLabels, ToolbarTabStyle, GUI.ToolbarButtonSize.FitToContents);
            switch (eventToolbarInt)
            {
                // Peer joined
                case 0:
                    {
                        OdinHandler.Config.PeerJoinedEvent = EditorGUILayout.Toggle(new GUIContent("Enabled", "Toggles the peer joined event"), OdinHandler.Config.PeerJoinedEvent);
                        EditorGUILayout.PropertyField(OnPeerJoined, new GUIContent("OnPeerJoined", "Setup the peer joined event"));
                        break;
                    }
                // Peer left
                case 1:
                    {
                        OdinHandler.Config.PeerLeftEvent = EditorGUILayout.Toggle(new GUIContent("Enabled", "Toggles the peer left event"), OdinHandler.Config.PeerLeftEvent);
                        EditorGUILayout.PropertyField(OnPeerLeft, new GUIContent("OnPeerLeft", "Setup the peer left event"));
                        break;
                    }
                // Peer updated
                case 2:
                    {
                        OdinHandler.Config.PeerUpdatedEvent = EditorGUILayout.Toggle(new GUIContent("Enabled", "Toggles the peer updated event"), OdinHandler.Config.PeerUpdatedEvent);
                        EditorGUILayout.PropertyField(OnPeerUpdated, new GUIContent("OnPeerUpdated", "Setup the peer updated event"));
                        break;
                    }
                // Media added
                case 3:
                    {
                        OdinHandler.Config.MediaAddedEvent = EditorGUILayout.Toggle(new GUIContent("Enabled", "Toggles the media added event"), OdinHandler.Config.MediaAddedEvent);
                        EditorGUILayout.PropertyField(OnMediaAdded, new GUIContent("OnMediaAdded", "Setup the media added event"));
                        break;
                    }
                // Media removed
                case 4:
                    {
                        OdinHandler.Config.MediaRemovedEvent = EditorGUILayout.Toggle(new GUIContent("Enabled", "Toggles the media removed event"), OdinHandler.Config.MediaRemovedEvent);
                        EditorGUILayout.PropertyField(OnMediaRemoved, new GUIContent("OnMediaRemoved", "Setup the media removed event"));
                        break;
                    }
            }
            #endregion SubRoomEvents
        }
    }
    #endregion

    private void CreateHandlerSettingsLayout()
    {
        toggleHandlerSettings = EditorGUILayout.Foldout(toggleHandlerSettings, "Handler Settings", FoldoutLabelStyle);
        drawRect(2);
        if (toggleHandlerSettings)
        {
            EditorGUILayout.PropertyField(Audio3D, new GUIContent("Manual positional audio", "Setup positional audio PlaybackStreams for manual use (mutually exclusive to \"Playback auto creation\")"));
            EditorGUILayout.PropertyField(PlaybackCreation, new GUIContent("Playback auto creation", "Automatically creates Playback components within the handler object (mutually exclusive to \"Manual positional audio\")"));
            EditorGUILayout.PropertyField(AudioMixerObject, new GUIContent("Playback Audio-Mixer Group", "Link to Unity AudioMixer"));
        }
    }
}
#endif