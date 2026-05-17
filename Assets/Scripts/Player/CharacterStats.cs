using System;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float attack;
    [SerializeField] protected float speed;

    private float _health;
    private float _block;

    public float GetSpeed => speed;
    public float CurrentHealth => _health;

    public event Action OnHealthChange;
    private void HealthChanged()
    {
        OnHealthChange?.Invoke();
    }

    private void Start()
    {
        _health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        float damageDone = damage;

        if (_block > 0)
        {
            _block -= damage;

            if (_block < 0)
            {
                damageDone = -_block;
            }
        }

        _health -= damageDone;
        HealthChanged();

        if (_health <= 0)
        {
            _health = 0;
            //Character Death Function
        }
    }

    public void ReceiveHealing(float healing)
    {
        _health += healing;
        HealthChanged();

        if (_health > maxHealth)
        {
            _health = maxHealth;
        }
    }

    public void ReceiveBlocking(float block)
    {
        _block += block;
    }

    public void GetPowered(StatusCard powerUpCard)
    {
        powerUpCard.Apply(this);
    }

    public void GetUnpowered(StatusCard powerDownCard)
    {
        powerDownCard.Apply(this);
    }
}