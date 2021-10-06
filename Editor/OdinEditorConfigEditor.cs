using OdinNative.Unity;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
/// <summary>
/// Adds a custom layout to the OdinEditorConfig component
/// </summary>
[CustomEditor(typeof(OdinEditorConfig))]
public class OdinEditorConfigEditor : Editor
{
    public Texture2D myGUITexture;

    SerializedProperty Verbose;
    SerializedProperty AccessKey;
    SerializedProperty Server;
    SerializedProperty UserDataText;
    
    SerializedProperty DeviceSampleRate;
    SerializedProperty DeviceChannels;
    SerializedProperty RemoteSampleRate;
    SerializedProperty RemoteChannels;
    
    SerializedProperty PeerJoinedEvent;
    SerializedProperty PeerLeftEvent;
    SerializedProperty PeerUpdatedEvent;
    SerializedProperty MediaAddedEvent;
    SerializedProperty MediaRemovedEvent;
    
    SerializedProperty VadEnable;
    SerializedProperty EchoCanceller;
    SerializedProperty HighPassFilter;
    SerializedProperty PreAmplifier;
    SerializedProperty NoiseSuppressionLevel;
    SerializedProperty TransientSuppressor;
    
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
        
        PeerJoinedEvent = serializedObject.FindProperty("PeerJoinedEvent");
        PeerLeftEvent = serializedObject.FindProperty("PeerLeftEvent");
        PeerUpdatedEvent = serializedObject.FindProperty("PeerUpdatedEvent");
        MediaAddedEvent = serializedObject.FindProperty("MediaAddedEvent");
        MediaRemovedEvent = serializedObject.FindProperty("MediaRemovedEvent");
        
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
        OdinEditorConfig odinEditorConfig = (target as OdinEditorConfig);
        if (odinEditorConfig == null)
        {
            return;
        }

        if (!myGUITexture)
        {
            Debug.LogError("Missing texture, assign a texture in the inspector");
        }
    
        EditorStyles.boldLabel.normal.textColor = Color.cyan;
        GUILayout.Box(myGUITexture, GUILayout.ExpandWidth(true), GUILayout.Height(100));
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Documentation"))
        {
            Application.OpenURL("https://developers.4players.io/odin/");
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.PropertyField(Verbose, new GUIContent("Verbose", "Enable additional logs"));
        
        CreateClientSettingsLayout();
        CreateAudioSettingsLayout();
        CreateEventListenersLayout();
        CreateRoomSettingsLayout();
        serializedObject.ApplyModifiedProperties();
    }
    
    #region AudioSettings
    private void CreateClientSettingsLayout()
    {
        GUILayout.Label("Client-Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(AccessKey, new GUIContent("Access Key", "Grants access to the ODIN-Network for a customer"));
        EditorGUILayout.PropertyField(Server, new GUIContent("Server", "Server URL"));
        EditorGUILayout.PropertyField(UserDataText, new GUIContent("User data", "Custom user data"));
    }
    #endregion

    #region ClientSettings
    private void CreateAudioSettingsLayout()
    {
        GUILayout.Label("Audio-Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(DeviceSampleRate, new GUIContent("Capture device sample rate", "Sets the sample rate of the capture device"));
        EditorGUILayout.PropertyField(DeviceChannels, new GUIContent("Capture device channels", "Sets the channels of the capture device"));
        EditorGUILayout.PropertyField(RemoteSampleRate, new GUIContent("Server sample rate", "Sets the sample rate of the server media steam"));
        EditorGUILayout.PropertyField(RemoteChannels, new GUIContent("Server channels", "Sets the channels of the server"));
    }
    #endregion

    #region EventListeners
    private void CreateEventListenersLayout()
    {
        GUILayout.Label("Event Listeners", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(PeerJoinedEvent, new GUIContent("Peer joined", "Toggles the peer joined event"));
        EditorGUILayout.PropertyField(PeerLeftEvent, new GUIContent("Peer left", "Toggles the peer left event"));
        EditorGUILayout.PropertyField(PeerUpdatedEvent, new GUIContent("Peer updated", "Toggles the peer updated event"));
        EditorGUILayout.PropertyField(MediaAddedEvent, new GUIContent("Media added", "Toggles the media added event"));
        EditorGUILayout.PropertyField(MediaRemovedEvent, new GUIContent("Media removed", "Toggles the media removed event"));
    }
    #endregion

    #region RoomSettings
    private void CreateRoomSettingsLayout()
    {
        GUILayout.Label("Standard room settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(VadEnable, new GUIContent("Voice activity detection", "Turns VAC on and off"));
        EditorGUILayout.PropertyField(EchoCanceller, new GUIContent("Echo cancellation", "Turns Echo cancellation on and off"));
        EditorGUILayout.PropertyField(HighPassFilter, new GUIContent("High pass filter", "Reduces lower frequencies of the input (Automatic game control)"));
        EditorGUILayout.PropertyField(PreAmplifier, new GUIContent("Input amplifier", "Amplifies the audio input"));
        EditorGUILayout.PropertyField(NoiseSuppressionLevel, new GUIContent("Noise suppression", "Filters background noises"));
        EditorGUILayout.PropertyField(TransientSuppressor, new GUIContent("Transient suppression", "Filters high amplitude noices"));
    }
    #endregion
}
#endif