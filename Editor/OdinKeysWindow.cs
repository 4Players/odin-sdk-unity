#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace OdinNative.Unity.UIEditor
{
    public class OdinKeysWindow : EditorWindow
    {
        public StyleSheet styleSheet;
        public string AccessKey;
        public string Gateway = "gateway.odin.4players";
        private TextElement accessKeyTextElement;

        [MenuItem("Window/4Players ODIN/Manage Access", false, 0)]
        public static void ShowWindow()
        {
            OdinKeysWindow window = GetWindow<OdinKeysWindow>();
            window.minSize = new Vector2(440, 220);
            window.maxSize = window.minSize;
            window.titleContent = new GUIContent("Access Generator");
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.styleSheets.Add(styleSheet);

            // Headline
            Label headlineLabel = new Label("Manage Access to 4Players ODIN");
            headlineLabel.AddToClassList("headline");
            root.Add(headlineLabel);

            if (string.IsNullOrEmpty(AccessKey) == false)
            {
                // Displays no config found error
                VisualElement innerContainerNoConfig = new VisualElement();
                innerContainerNoConfig.AddToClassList("inner-container");
                root.Add(innerContainerNoConfig);
                Label noConfigFoundLabel = new Label("ODIN configuration is not available");
                noConfigFoundLabel.AddToClassList("row-label-error");
                innerContainerNoConfig.Add(noConfigFoundLabel);
                return;
            }

            // Renders the gateway box
            VisualElement innerContainerGateway = new VisualElement();
            innerContainerGateway.AddToClassList("inner-container");
            root.Add(innerContainerGateway);
            Label gatewayLabel = new Label("Using ODIN Gateway:");
            gatewayLabel.AddToClassList("row-label");
            TextElement gatewayText = new TextElement();
            gatewayText.AddToClassList("key");
            innerContainerGateway.Add(gatewayLabel);
            innerContainerGateway.Add(gatewayText);
            gatewayText.text = Gateway;

            // Renders the access key box
            VisualElement innerContainerKey = new VisualElement();
            innerContainerKey.AddToClassList("inner-container");
            root.Add(innerContainerKey);
            Label accessKeyLabel = new Label("Your Access Key:");
            accessKeyLabel.AddToClassList("row-label");
            accessKeyTextElement = new TextElement();
            accessKeyTextElement.AddToClassList("key");
            accessKeyTextElement.text = String.IsNullOrEmpty(AccessKey) ? "- none -" : AccessKey;
            innerContainerKey.Add(accessKeyLabel);
            innerContainerKey.Add(accessKeyTextElement);

            // Renders the generate access key button
            Button accessKeyBtn = new Button();
            accessKeyBtn.clicked += OnClickAccessBtn;
            accessKeyBtn.AddToClassList("access-key-btn");
            accessKeyBtn.text = "Generate Access Key";
            innerContainerKey.Add(accessKeyBtn);

            Button copyBtn = new Button();
            copyBtn.clicked += CopyBtn_clicked;
            copyBtn.AddToClassList("access-key-btn");
            copyBtn.text = "Copy Key";
            innerContainerKey.Add(copyBtn);
        }

        private void CopyBtn_clicked()
        {
            GUIUtility.systemCopyBuffer = AccessKey;
        }

        public void OnClickAccessBtn()
        {
            AccessKey = GenerateAccessKey();
            accessKeyTextElement.text = AccessKey;
            Debug.Log(AccessKey);
        }

        internal static string GenerateAccessKey()
        {
            var rand = new System.Random();
            byte[] key = new byte[33];
            rand.NextBytes(key);
            key[0] = 0x01;
            byte[] subArray = new ArraySegment<byte>(key, 1, 31).ToArray();
            key[32] = Crc8(subArray);
            return Convert.ToBase64String(key);
        }

        private static byte Crc8(byte[] data)
        {
            byte crc = 0xff;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x80) != 0) crc = (byte)((crc << 1) ^ 0x31);
                    else crc <<= 1;
                }
                crc = (byte)(0xff & crc);
            }
            return crc;
        }
    }
}
#endif