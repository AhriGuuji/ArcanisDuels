using UnityEngine;

public class Heal : Card
{
    [SerializeField] private float heal = 50;

    public override float Effect(CharacterStats target)
    {
        return heal;
    }
}
