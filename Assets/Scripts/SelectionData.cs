using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SelectionData", menuName = "ScriptableObjects/SelectionData")]
public class SelectionData : ScriptableObject
{
    public string prefabName;
    public Deck deck;
}