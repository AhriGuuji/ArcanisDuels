using System;
using Unity.Netcode;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    protected NetworkVariable<float> maxHealth = new NetworkVariable<float>(150f);
    protected NetworkVariable<float> attack = new NetworkVariable<float>(0.2f);
    protected NetworkVariable<float> speed = new NetworkVariable<float>(10f);
    private Animator _anim;

    private NetworkVariable<float> _health = new NetworkVariable<float>(0f);
    private NetworkVariable<float> _block = new NetworkVariable<float>(0f);

    public float GetSpeed => speed.Value;
    public float CurrentHealth => _health.Value;
    public float GetAttack => attack.Value;

    public event Action OnHealthChange;
    private void HealthChanged()
    {
        OnHealthChange?.Invoke();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(IsServer)
        {
            _health.Value = maxHealth.Value;
            _block.Value = 0;
        }
        
        _health.OnValueChanged += (_, newValue) => OnHealthChange?.Invoke();
        
        _anim = GetComponent<Animator>();
    }

    public void TakeDamage(float damage)
    {
        if(!IsServer) return;

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
        if(!IsServer) return;

        _health.Value += healing;

        if (_health.Value > maxHealth.Value)
        {
            _health.Value = maxHealth.Value;
        }

        HealthChanged();
    }

    public void ReceiveBlocking(float block)
    {
        if(!IsServer) return;

        _block.Value += block;
    }

    public void ClearBlock()
    {
        if(!IsServer) return;

        _block.Value = 0;
    }

    public void GetPowered(StatusCard powerUpCard)
    {
        if(!IsServer) return;

        powerUpCard.Apply(this);
    }

    public void GetUnpowered(StatusCard powerDownCard)
    {
        if(!IsServer) return;

        powerDownCard.Apply(this);
    }

    public void PlayAnimation(CardType card)
    {
        if (_anim == null) return;
        
        switch(card)
            {
                case CardType.Damage:
                    {
                        _anim.Play("Take");
                        break;
                    }
                case CardType.Heal:
                    {
                        _anim.Play("Heal");
                        break;
                    }
                case CardType.Block:
                    {
                        _anim.Play("Block");
                        break;
                    }
                case CardType.PowerUp:
                    {
                        _anim.Play("PowerUp");
                        break; 
                    }
                case CardType.PowerDown:
                    {
                        _anim.Play("PowerDown");
                        break;
                    }
            }
    }
}