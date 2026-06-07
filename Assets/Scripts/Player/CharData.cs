using UnityEngine;

[CreateAssetMenu(fileName = "CharData", menuName = "Character/CharData")]
public class CharData : ScriptableObject
{
    [Header("Character Stats")]
    [field: SerializeField] public float maxHealth { get; private set; }
    [field: SerializeField] public float attack { get; private set; }
    [field: SerializeField] public float speed { get; private set; }
}