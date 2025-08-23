#if UNITY_EDITOR
using System.Collections.Generic;
using OdinNative.Odin;
using UnityEditor;
using OdinNative.Unity;
using OdinNative.Odin.Room;
using UnityEditor.VSAttribution.Odin;
using UnityEngine;

namespace UnityEditor.VSAttribution.Odin
{
    [InitializeOnLoad]
    public static class OdinRoomJoinListener
    {
        private static readonly string AttributionActionName = "OdinJoin";
        private static readonly string AttributionPartnerName = "io.fourplayers.odin.pre";
        
        static OdinRoomJoinListener()
        {
            OdinHandler.OnInstanceInitialized += OnOdinHandlerInstanceInitialized;
        }

        private static void OnOdinHandlerInstanceInitialized()
        {
            if (OdinHandler.Instance)
            {
                OdinHandler.Instance.OnRoomJoined.AddListener(Editor_OnJoined);
            }
        }

        private static void Editor_OnJoined(RoomJoinedEventArgs args)
        {
            if (args.Room.IsJoined)
            {
                string customerId = args.Room.GetRoomCustomer();
                if (GetUIdf(customerId, out string fId))
                {
                    OdinVSAttribution.SendAttributionEvent(AttributionActionName, AttributionPartnerName, fId);
                }
            }
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
    }
}

#endif