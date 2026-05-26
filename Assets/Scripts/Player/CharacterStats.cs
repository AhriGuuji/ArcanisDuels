using System;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    [SerializeField] protected NetworkVariable<float> maxHealth;
    [SerializeField] protected NetworkVariable<float> attack;
    [SerializeField] protected NetworkVariable<float> speed;
    protected NetworkObject networkObject;

    private NetworkVariable<float> _health;
    private NetworkVariable<float> _block;

    public float GetSpeed => speed.Value;
    public float CurrentHealth => _health.Value;

    public event Action OnHealthChange;
    private void HealthChanged()
    {
        OnHealthChange?.Invoke();
    }

    private void Start()
    {
        _health.Value = maxHealth.Value;
        networkObject = GetComponent<NetworkObject>();
    }

    public void TakeDamage(float damage)
    {
        float damageDone = damage;

        if (_block.Value > 0)
        {
            _block.Value -= damage;

            if (_block.Value < 0)
            {
                damageDone = -_block.Value;
            }
        }

        _health.Value -= damageDone;
        HealthChanged();

        if (_health.Value <= 0)
        {
            _health.Value = 0;
            //Character Death Function
        }
    }

    public void ReceiveHealing(float healing)
    {
        _health.Value += healing;
        HealthChanged();

        if (_health.Value > maxHealth.Value)
        {
            _health.Value = maxHealth.Value;
        }
    }

    public void ReceiveBlocking(float block)
    {
        _block.Value += block;
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