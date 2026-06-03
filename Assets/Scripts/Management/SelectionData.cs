using System.Collections.Generic;
using UnityEngine;

public static class SelectionData
{
    public readonly static string playerName = PlayerPrefs.GetString("PlayerName");
    public static string prefabName;
    public static List<int> deck = new(); 
    public static bool isServer = false;
    public static string code;
}