using System;

[Flags]
public enum CardType
{
    None = 0,
    Damage = 1 << 0,
    Heal = 1 << 1,
    Block = 1 << 2,
    PowerUp = 1 << 3,
    PowerDown = 1 << 4
}