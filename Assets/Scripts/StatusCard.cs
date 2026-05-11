using UnityEngine;

public abstract class StatusCard : Card
{
    public abstract void Apply(CharacterStats player);
    public abstract void Remove(CharacterStats player);
    public abstract string StatusName{ get; protected set; }
}