using UnityEngine;

public class Quick : Card
{
    [SerializeField] private float damage = 20;

    public override float Effect(CharacterStats target)
    {
        return damage;
    }
}
