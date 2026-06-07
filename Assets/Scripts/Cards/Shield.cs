using UnityEngine;

public class Shield : Card
{
    [SerializeField] private float shieldAmount = 50;

    public override float Effect(CharacterStats target)
    {
        return shieldAmount;
    }
}
