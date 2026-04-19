public class Burn : ExtraEffect
{
    private float _damage;
    public Burn(CharacterStats target, int duration, float damage) : base(target, duration)
    {
        _damage = damage;
    }

    public override void Effect()
    {
        _actualTurn++;
        _target.TakeDamage(_damage);

        if (_actualTurn == _duration)
            BattleManager.RemoveExtra(this);
    }
}