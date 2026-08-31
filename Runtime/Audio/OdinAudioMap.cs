using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OdinNative.Core.Utility;

[ExecuteInEditMode]
[AddComponentMenu("Odin/Audio/OdinAudioMap")]
public class OdinAudioMap : MonoBehaviour
{
    [Flags]
    public enum MapType
    {
        All = -1,
        None = 0,
        Encoder = 1,
        Decoder = 2
    }

    public string Name;
    public MapType MapSelector;
    [NonReorderable]
    public string[] ChannelKeys;
    [NonReorderable]
    public bool[] ChannelValues;

    public ChannelMask ChannelMask { get { return SelectedChannelMask(); } }
    public string Channels { get { return SelectedChannels(); } }

    private void Awake()
    {
        // serialized components always carry non-null arrays, so this only initializes
        // a runtime AddComponent where Reset() never ran - configured mappings stay untouched
        if (ChannelKeys == null || ChannelValues == null)
            SetDefaults();
    }

    private void Reset()
    {
        SetDefaults();
    }

    private void SetDefaults()
    {
        MapSelector = MapType.All;
        ChannelKeys = Enum.GetNames(typeof(ChannelMask));
        ChannelValues = new bool[ChannelKeys.Length];
        ChannelValues[1] = true;
    }

    public ChannelMask SelectedChannelMask()
    {
        ChannelMask result = ChannelMask.None;

        foreach(var kvp in ChannelKeys
            .Zip(ChannelValues, (k, v) => new { k, v })
            .ToDictionary(x => x.k, x => x.v)
            .Where(kvp => kvp.Value))
            result |= (ChannelMask)Enum.Parse(typeof(ChannelMask), kvp.Key);

        return result;
    }

    public string SelectedChannels()
    {
        return string.Join(" | ", ChannelKeys
            .Zip(ChannelValues, (k, v) => new { k, v })
            .ToDictionary(x => x.k, x => x.v)
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key));
    }

    public override string ToString()
    {
        return Name;
    }
}
