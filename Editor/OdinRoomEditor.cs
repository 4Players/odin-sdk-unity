using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using UnityEditor;
using UnityEditor.VSAttribution.Odin;
using UnityEngine;

namespace OdinNative.Unity.UIEditor
{
    [CustomEditor(typeof(OdinNative.Unity.OdinRoom))]
    public class OdinRoomEditor : Editor
    {
        private static readonly string AttributionActionName = "OdinJoin";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        void OnEnable()
        {
#if UASOdin
            ((OdinRoom)target).OnRoomJoined.AddListener(Editor_OdinJoined);
#endif
        }

        private static void Editor_OdinJoined(object sender, RoomJoinedEventArgs args)
        {
            if (GetUIdf(args.Customer, out string fId))
                OdinVSAttribution.SendAttributionEvent(AttributionActionName, OdinDefaults.SDKID, fId);
        }

        private static bool GetUIdf(string customer, out string fid)
        {
            fid = string.Empty;
#if !UASOdin
            return false;
#else
            if (string.IsNullOrEmpty(customer)) return false; // argument error
            if (OdinDefaults.Debug)
            {
                Debug.Log($"unique {customer}");
                return false;
            }

            // get fId
            string[] selection = customer.Split("_");
            if (selection.Length != 3) return false; // format error
            if (selection[0] != "customer") return false; // invalid data
            fid = selection[1];
            return string.IsNullOrEmpty(fid) == false;
#endif
        }

        void OnDisable()
        {
#if UASOdin
            ((OdinRoom)target).OnRoomJoined.RemoveListener(Editor_OdinJoined);
#endif
        }
    }
}
