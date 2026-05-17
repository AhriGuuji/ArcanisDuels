using UnityEngine;

public class FireBall : Card
{
    [SerializeField] private float damage = 20;
    [SerializeField] private float burnDamage = 5;
    [SerializeField] private int burnDuration = 3;
    public override float Effect(CharacterStats target)
    {
        BattleManager.AddExtra(new Burn(target,burnDuration,burnDamage));

        return damage;
    }
}