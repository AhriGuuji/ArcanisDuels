using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [field: SerializeField] public CharacterStats Owner { get; protected set; }
    [field: SerializeField] public bool Priority { get; protected set; }
    [field: SerializeField] public CardType Type { get; protected set; }
    public BattleManager BattleManager { get; set; }

    public abstract float Effect(CharacterStats target); 
}