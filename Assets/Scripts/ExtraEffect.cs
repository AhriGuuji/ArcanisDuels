public abstract class ExtraEffect
{
    protected CharacterStats _target;
    protected int _duration;
    protected int _actualTurn;
    public ExtraEffect(CharacterStats target, int duration)
    {
        _target = target;
        _duration = duration;
        _actualTurn = 0;
    }

    public abstract void Effect();

    public BattleManager BattleManager { get; set; }
}