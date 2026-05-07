using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [field: SerializeField] public CharacterStats Owner { get; protected set; }
    [field: SerializeField] public bool Priority { get; protected set; }
    [field: SerializeField] public CardType Type { get; protected set; }
    public BattleManager BattleManager { get; set; }
    public string Name => GetType().ToString();

    public abstract float Effect(CharacterStats target); 
    public void SetOwner(CharacterStats owner) => Owner = owner;
}