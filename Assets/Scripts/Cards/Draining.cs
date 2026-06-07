using UnityEngine;

public class Draining : Card
{
    [SerializeField] private float damage = 30;
    [SerializeField] private float heal = 15;

    public override float Effect(CharacterStats target)
    {
        if (target == Owner)
            return heal;
        else
            return damage;
    }
}
