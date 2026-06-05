using UnityEngine;

public class IceComet : Card
{
    [SerializeField] private float damage = 60;

    public override float Effect(CharacterStats target)
    {
        return damage;
    }
}