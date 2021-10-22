#if UNITY_EDITOR
using OdinNative.Unity;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Adds a custom layout to the OdinEditorConfig component
/// </summary>
[CustomEditor(typeof(OdinEditorConfig))]
public class OdinEditorConfigEditor : Editor
{
    SerializedProperty Verbose;
    SerializedProperty AccessKey;
    SerializedProperty Server;
    SerializedProperty UserDataText;
    
    SerializedProperty DeviceSampleRate;
    SerializedProperty DeviceChannels;
    SerializedProperty RemoteSampleRate;
    SerializedProperty RemoteChannels;
    
    SerializedProperty PeerJoinedEventToggle;
    SerializedProperty PeerLeftEventToggle;
    SerializedProperty PeerUpdatedEventToggle;
    SerializedProperty MediaAddedEventToggle;
    SerializedProperty MediaRemovedEventToggle;

    SerializedProperty VadEnable;
    SerializedProperty EchoCanceller;
    SerializedProperty HighPassFilter;
    SerializedProperty PreAmplifier;
    SerializedProperty NoiseSuppressionLevel;
    SerializedProperty TransientSuppressor;

    private bool toggleClientSettings;
    private bool toggleAudioSettings;
    private bool toggleEventListeners;
    private bool toggleRoomSettings;

    private GUIStyle FoldoutLabelStyle;

    void OnEnable()
    {
        Verbose = serializedObject.FindProperty("Verbose");

        AccessKey = serializedObject.FindProperty("AccessKey");
        Server = serializedObject.FindProperty("Server");
        UserDataText = serializedObject.FindProperty("UserDataText");
        
        DeviceSampleRate = serializedObject.FindProperty("DeviceSampleRate");
        DeviceChannels = serializedObject.FindProperty("DeviceChannels");
        RemoteSampleRate = serializedObject.FindProperty("RemoteSampleRate");
        RemoteChannels = serializedObject.FindProperty("RemoteChannels");

        PeerJoinedEventToggle = serializedObject.FindProperty("PeerJoinedEvent");
        PeerLeftEventToggle = serializedObject.FindProperty("PeerLeftEvent");
        PeerUpdatedEventToggle = serializedObject.FindProperty("PeerUpdatedEvent");
        MediaAddedEventToggle = serializedObject.FindProperty("MediaAddedEvent");
        MediaRemovedEventToggle = serializedObject.FindProperty("MediaRemovedEvent");

        VadEnable = serializedObject.FindProperty("VadEnable");
        EchoCanceller = serializedObject.FindProperty("EchoCanceller");
        HighPassFilter = serializedObject.FindProperty("HighPassFilter");
        PreAmplifier = serializedObject.FindProperty("PreAmplifier");
        NoiseSuppressionLevel = serializedObject.FindProperty("NoiseSuppressionLevel");
        TransientSuppressor = serializedObject.FindProperty("TransientSuppressor");
    }
    
    /// <summary>
    /// Implementation for the Unity custom inspector of OdinEditorConfig.
    /// </summary>
    public override void OnInspectorGUI()
    {
        changeStyles();
        OdinEditorConfig odinEditorConfig = (target as OdinEditorConfig);
        if (odinEditorConfig == null)
        {
            DrawDefaultInspector(); // fallback
            return;
        }

        EditorGUILayout.PropertyField(Verbose, new GUIContent("Verbose", "Enable Additional Logs"));
        GUILayout.Space(10);
        CreateClientSettingsLayout(odinEditorConfig);
        GUILayout.Space(10);
        CreateAudioSettingsLayout();
        GUILayout.Space(10);
        CreateEventListenersLayout();
        GUILayout.Space(10);
        CreateRoomSettingsLayout();
        serializedObject.ApplyModifiedProperties();
        
    }

    private void changeStyles()
    {
        FoldoutLabelStyle = new GUIStyle(EditorStyles.foldout);
        FoldoutLabelStyle.fontStyle = FontStyle.Bold;
        FoldoutLabelStyle.fontSize = 14;
    }

    private static void drawRect(int height)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height );
        rect.height = height;
        EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
        GUILayout.Space(3);
        
    }
    
    #region ClientSettings
    private void CreateClientSettingsLayout(OdinEditorConfig odinEditorConfig)
    {
        toggleClientSettings = EditorGUILayout.Foldout(toggleClientSettings, "Client Settings", FoldoutLabelStyle);
        drawRect(2);
        if (toggleClientSettings)
        {
            EditorGUILayout.PropertyField(AccessKey, new GUIContent("Access Key", "Used to create room tokens for accessing the ODIN network. \n \nNote that all of your clients must use tokens generated from either the same access key or another key from the same project. While you can create an infinite number of access keys for your projects, we strongly recommend that you never put an Access key in your client code."));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Manage Access"))
            {
                OdinKeysWindow.ShowWindow();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(UserDataText, new GUIContent("User data", "Custom user data"));   
        }

    }
    #endregion

    #region AudioSettings
    private void CreateAudioSettingsLayout()
    {

        toggleAudioSettings = EditorGUILayout.Foldout(toggleAudioSettings, "Audio Settings", FoldoutLabelStyle);
        drawRect(2);
        if (toggleAudioSettings)
        {
            EditorGUILayout.PropertyField(DeviceSampleRate, new GUIContent("Capture device sample rate", "Sets the sample rate of the capture device"));
            EditorGUILayout.PropertyField(DeviceChannels, new GUIContent("Capture device channels", "Sets the channels of the capture device"));
            EditorGUILayout.PropertyField(RemoteSampleRate, new GUIContent("Server sample rate", "Sets the sample rate of the server media steam"));
            EditorGUILayout.PropertyField(RemoteChannels, new GUIContent("Server channels", "Sets the channels of the server"));
        }
    }
    #endregion

    #region EventListeners
    private void CreateEventListenersLayout()
    {
        toggleEventListeners = EditorGUILayout.Foldout(toggleEventListeners, "Event Listeners", FoldoutLabelStyle);
        drawRect(2);
        if (toggleEventListeners)
        {
            EditorGUILayout.PropertyField(PeerJoinedEventToggle, new GUIContent("OnPeerJoined", "Enable/Disable the peer joined event"));
            EditorGUILayout.PropertyField(PeerLeftEventToggle, new GUIContent("OnPeerLeft", "Enable/Disable the peer left event"));
            EditorGUILayout.PropertyField(PeerUpdatedEventToggle, new GUIContent("OnPeerUpdated", "Enable/Disable the peer updated event"));
            EditorGUILayout.PropertyField(MediaAddedEventToggle, new GUIContent("OnMediaAdded", "Enable/Disable the media added event"));
            EditorGUILayout.PropertyField(MediaRemovedEventToggle, new GUIContent("OnMediaRemoved", "Enable/Disable the media removed event"));
        }
    }
    #endregion

    #region RoomSettings
    private void CreateRoomSettingsLayout()
    {
        toggleRoomSettings = EditorGUILayout.Foldout(toggleRoomSettings, "Room Settings", FoldoutLabelStyle);
        drawRect(2);
        if (toggleRoomSettings)
        {
            EditorGUILayout.PropertyField(VadEnable, new GUIContent("Voice activity detection", "Turns VAC on and off"));
            //EditorGUILayout.PropertyField(EchoCanceller, new GUIContent("Echo cancellation", "Turns Echo cancellation on and off"));
            EditorGUILayout.PropertyField(HighPassFilter, new GUIContent("High pass filter", "Reduces lower frequencies of the input"));
            EditorGUILayout.PropertyField(PreAmplifier, new GUIContent("Input amplifier", "Amplifies the audio input"));
            EditorGUILayout.PropertyField(NoiseSuppressionLevel, new GUIContent("Noise suppression", "Filters background noises"));
            EditorGUILayout.PropertyField(TransientSuppressor, new GUIContent("Transient suppression", "Filters high amplitude noises"));
        }
    }
    #endregion
}
#endif